using Calendar.Data;
using Calendar.Models;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Controllers;

public class TaskController(DB_Service db) : Controller
{
    #region GET Methods
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Details()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Edit()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie))
            return Unauthorized(new { message = "Se requiere autenticación." });
        var user = await _db.GetUserById(Guid.Parse(userIdCookie));
        if(user == null)
            return NotFound(new { message = "Usuario no encontrado." });
        return Ok(user.ContainerTasks.Where(t => t is WorkTask).Cast<WorkTask>());
    }

    [HttpGet]
    public async Task<IActionResult> GetTasksCalendar()
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie)) return Unauthorized(new { message = "Se requiere autenticación." }); 

        var user = await _db.GetUserById(Guid.Parse(userIdCookie));
        if(user == null) return Unauthorized(new { message = "Usuario no encontrado." });

        var dailySchedule = user.Schedule;
        if (dailySchedule == null) return BadRequest("No tienes un horario configurado.");

        var workTasks = user.ContainerTasks.OfType<WorkTask>().OrderByDescending(t => t.Priority).ToList();
        var breakTasks = user.ContainerTasks.OfType<BreakTask>().ToList();

        DateTime todayUnspecified = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
        var calendarEvents = new List<object>(); 
        var scheduledDates = new HashSet<DateTime>(); 
        DateTime currentDay = todayUnspecified; 
        DateTime cursor = currentDay.Add(dailySchedule.StartTime.ToTimeSpan());

        foreach (var task in workTasks)
        {
            double hoursRemaining = task.Hours;

            while (hoursRemaining > 0)
            {
                scheduledDates.Add(currentDay);

                DateTime dayEnd = currentDay.Add(dailySchedule.EndTime.ToTimeSpan());

                if (cursor >= dayEnd)
                {
                    currentDay = currentDay.AddDays(1);
                    cursor = currentDay.Add(dailySchedule.StartTime.ToTimeSpan());
                    dayEnd = currentDay.Add(dailySchedule.EndTime.ToTimeSpan());
                    scheduledDates.Add(currentDay); 
                }

                // Se busca si hay un conflicto con alguna tarea Break
                var nextBreak = breakTasks
                    .Select(b => new { 
                        Original = b,
                        Start = currentDay.Add(b.Start.ToTimeSpan()), 
                        End = currentDay.Add(b.End.ToTimeSpan()) 
                    })
                    .Where(b => b.Start > cursor && b.Start < dayEnd) 
                    .OrderBy(b => b.Start)
                    .FirstOrDefault();

                DateTime slotLimit = (nextBreak != null) ? nextBreak.Start : dayEnd;
                TimeSpan availableTime = slotLimit - cursor;

                if (availableTime.TotalMinutes <= 0)
                {
                    if (nextBreak != null && cursor >= nextBreak.Start && cursor < nextBreak.End)
                        cursor = nextBreak.End;
                    else 
                        cursor = dayEnd; 
                    continue; 
                }

                double hoursToAllocate = Math.Min(availableTime.TotalHours, hoursRemaining);

                calendarEvents.Add(new 
                {
                    id = task.Id,
                    title = task.Title,
                    category = task.Category,
                    start = cursor, 
                    end = cursor.AddHours(hoursToAllocate),
                });

                hoursRemaining -= hoursToAllocate;
                cursor = cursor.AddHours(hoursToAllocate);

                if (nextBreak != null && Math.Abs((cursor - nextBreak.Start).TotalMinutes) < 1)
                {
                    cursor = nextBreak.End;
                }
            }
        }

        // Se agregan las tareas Break al calendario
        foreach (var date in scheduledDates)
        {
            foreach (var brk in breakTasks)
            {
                calendarEvents.Add(new 
                {
                    id = brk.Id, 
                    title = brk.Title,
                    category = brk.Category,
                    start = date.Add(brk.Start.ToTimeSpan()),
                    end = date.Add(brk.End.ToTimeSpan()),
                });
            }
        }

        return Ok(new {calendarEvents, schedule = dailySchedule});
    }
    
    #endregion
    #region POST Methods
    [HttpPost]
    public async Task<IActionResult> Create(string task, DateOnly date, int hours)
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie))
        {
            ModelState.AddModelError("auth", "Se requiere autenticación.");
            return RedirectToAction("Login", "Home");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        if(date < today)
        {
            ModelState.AddModelError("date", "La fecha no puede ser menor a hoy.");
            return View();
        }

        var user = await _db.GetUserById(Guid.Parse(userIdCookie));
        if(user == null)
        {
            ModelState.AddModelError(string.Empty, "Usuario no encontrado.");
            return View();
        }
    
        WorkTask nuevaTarea = new()
        {
            Title = task,
            Deadline = date,
            Hours = hours,
            Category = "Work",
            Priority = CalculatePriority(date, hours)
        };

        CalculateSchedule(nuevaTarea, user);

        try {
            await _db.UpdateContainerTasks(Guid.Parse(userIdCookie), nuevaTarea);
        } catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        } catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> CreateBreakTask([FromBody] BreakTask breakTask)
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie))
        {
            ModelState.AddModelError("auth", "Se requiere autenticación.");
            return RedirectToAction("Login", "Home");
        }

        var user = await _db.GetUserById(Guid.Parse(userIdCookie));
        if(user == null)
        {
            ModelState.AddModelError(string.Empty, "Usuario no encontrado.");
            return View();
        }
    
        try {
            await _db.UpdateContainerTasks(Guid.Parse(userIdCookie), breakTask);
        } catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        } catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return RedirectToAction("Index");
    }


    #endregion
    #region PUT Methods
    [HttpPut("Task/UpdateTask/{id}")]
    public async Task<IActionResult> UpdateTask(string id, [FromBody] WorkTask updatedTask)
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie))
        {
            return Unauthorized(new { message = "Se requiere autenticación." });
        }

        var user = await _db.GetUserById(Guid.Parse(userIdCookie));
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }
        
        var task = user.ContainerTasks.FirstOrDefault(t => t.Title == updatedTask.Title && t.Id != Guid.Parse(id));
        if (task != null)
            return BadRequest(new { message = "Ya existe una tarea con ese título." });
        
        var taskToUpdate = user.ContainerTasks.FirstOrDefault(t => t.Id == Guid.Parse(id));
        if (taskToUpdate == null)
            return NotFound(new { message = "Tarea no encontrada." });
        
        await Create(updatedTask.Title, updatedTask.Deadline, updatedTask.Hours); // Creo la tarea actualizada
        await DeleteTask(id); // Elimino la tarea antigua

        return Ok();
    }

    [HttpPut("Task/UpdateBreakTask/{id}")]
    public async Task<IActionResult> UpdateBreakTask(string id, [FromBody] BreakTask updatedTask)
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie))
        {
            return Unauthorized(new { message = "Se requiere autenticación." });
        }

        var user = await _db.GetUserById(Guid.Parse(userIdCookie));
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }
        
        var task = user.ContainerTasks.FirstOrDefault(t => t.Title == updatedTask.Title && t.Id != Guid.Parse(id));
        if (task != null)
            return BadRequest(new { message = "Ya existe una tarea con ese título." });
        
        var taskToUpdate = user.ContainerTasks.FirstOrDefault(t => t.Id == Guid.Parse(id));
        if (taskToUpdate == null)
            return NotFound(new { message = "Tarea no encontrada." });
        
        await CreateBreakTask(updatedTask); // Creo la tarea actualizada
        await DeleteTask(id); // Elimino la tarea antigua

        return Ok();
    }


    #endregion
    #region DELETE Methods
    [HttpDelete("Task/DeleteTask/{id}")]
    public async Task<IActionResult> DeleteTask(string id)
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie)) return Unauthorized(new { message = "Se requiere autenticación." }); 

        var user = await _db.GetUserById(Guid.Parse(userIdCookie));
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });

        try
        {
            await _db.DeleteTask(Guid.Parse(userIdCookie), Guid.Parse(id));
            return NoContent();
        } catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        } catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }        
    }


    #endregion
    #region Private Logic
    private readonly DB_Service _db = db;
    private static double CalculatePriority(DateOnly date, int hours)
    {

        double daysleft = (date.ToDateTime(new TimeOnly()) - DateTime.Now).TotalDays;
        double priority = hours / (daysleft + 1);
        return priority;
    }

    private static WorkTask CalculateSchedule(WorkTask task, User user)
    {
        List<WorkTask> tasksUser = [.. user.ContainerTasks.Where(t => t is WorkTask).Cast<WorkTask>()];
        
        DateTime todayUnspecified = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

        if (tasksUser.Count == 0)
        {
            var result = user.Schedule.StartTime.AddHours(task.Hours);
            task.Start = todayUnspecified.Add(user.Schedule.StartTime.ToTimeSpan());
            task.End = todayUnspecified.Add(result.ToTimeSpan());
            return task;
        }
        else
        {
            List<WorkTask> allTasks = [.. tasksUser, task];
            allTasks.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            DateTime startTime = todayUnspecified.Add(user.Schedule.StartTime.ToTimeSpan());

            foreach (var t in allTasks)
            {
                t.Start = startTime;
                t.End = startTime.AddHours(t.Hours);
                startTime = t.End;
            }
        }
        return task;
    }
    #endregion
}
