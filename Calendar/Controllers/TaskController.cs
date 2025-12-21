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

        return Ok(user.ContainerTasks);
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
    
        TaskItem nuevaTarea = new()
        {
            Title = task,
            Deadline = date,
            Hours = hours,
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


    #endregion
    #region PUT Methods
    [HttpPut("Task/UpdateTask/{id}")]
    public async Task<IActionResult> UpdateTask(string id, [FromBody] TaskItem updatedTask)
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


    #endregion
    #region DELETE Methods
    [HttpDelete("Task/DeleteTask/{id}")]
    public async Task<IActionResult> DeleteTask(string id)
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

    private static TaskItem CalculateSchedule(TaskItem task, User user)
    {
        List<Schedule> horarios = user.Schedules;
        List<TaskItem> tasksUser = user.ContainerTasks;
        
        DateTime todayUnspecified = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

        if (tasksUser.Count == 0)
        {
            var result = horarios[0].StartTime.AddHours(task.Hours);
            task.Start = todayUnspecified.Add(horarios[0].StartTime.ToTimeSpan());
            task.End = todayUnspecified.Add(result.ToTimeSpan());
            return task;
        }
        else
        {
            List<TaskItem> allTasks = [.. tasksUser, task];
            allTasks.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            DateTime startTime = todayUnspecified.Add(horarios[0].StartTime.ToTimeSpan());

            foreach (var t in allTasks)
            {
                t.Start = startTime;
                t.End = startTime.AddHours(t.Hours);
                startTime = t.End;
            }
        }
        return task;
    }

    public async Task<IActionResult> GetTasksCalendar()
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie))
            return Unauthorized(new { message = "Se requiere autenticación." }); 

        var user = await _db.GetUserById(Guid.Parse(userIdCookie));
        if(user == null)
            return NotFound(new { message = "Usuario no encontrado." });

        var schedule = user.Schedules.OrderBy(s => s.StartTime).ToList();
        var tasks = user.ContainerTasks.OrderByDescending(t => t.Priority);

        if (schedule == null || tasks == null) return BadRequest("No se encontró el horario o las tareas.");

        int dayOffset = 0; // días desde hoy
        DateTime todayUnspecified = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
        var currentStart = todayUnspecified.AddDays(dayOffset).Add(schedule[0].StartTime.ToTimeSpan());
        var currentEnd = todayUnspecified.AddDays(dayOffset).Add(schedule[0].EndTime.ToTimeSpan());
        int scheduleIndex = 0;

        List<TaskItem> result = [];

        foreach (var task in tasks)
        {
            double hoursLeft = task.Hours;
            DateTime start = currentStart;

            while (hoursLeft > 0)
            {
                // Si estamos fuera del horario actual, pasamos al siguiente
                if (start >= currentEnd)
                {
                    scheduleIndex++;

                    if (scheduleIndex >= schedule.Count)
                    {
                        // Pasamos al siguiente día
                        scheduleIndex = 0;
                        dayOffset++;
                    }

                    start = todayUnspecified.AddDays(dayOffset).Add(schedule[scheduleIndex].StartTime.ToTimeSpan());
                    currentEnd = todayUnspecified.AddDays(dayOffset).Add(schedule[scheduleIndex].EndTime.ToTimeSpan());
                }

                // Calculamos cuántas horas quedan en esta franja
                var availableHours = (currentEnd - start).TotalHours;

                // Si la tarea cabe en esta franja
                if (hoursLeft <= availableHours)
                {
                    result.Add(new TaskItem
                    {
                        Title = task.Title,
                        Priority = task.Priority,
                        Hours = (int)hoursLeft,
                        Start = start,
                        End = start.AddHours(hoursLeft),
                        Deadline = task.Deadline
                    });

                    start = start.AddHours(hoursLeft);
                    currentStart = start;
                    hoursLeft = 0;
                }
                else
                {
                    // Dividimos la tarea en esta franja y seguimos en la siguiente
                    result.Add(new TaskItem
                    {
                        Title = task.Title,
                        Priority = task.Priority,
                        Hours = (int)availableHours,
                        Start = start,
                        End = currentEnd,
                        Deadline = task.Deadline
                    });

                    hoursLeft -= availableHours;
                    start = currentEnd; // Avanzamos a la siguiente franja o día
                }
            }
        }
        return Json(result);
    }  
    
    #endregion
}
