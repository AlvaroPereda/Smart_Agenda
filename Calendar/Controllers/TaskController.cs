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
    
    public async Task<List<TaskItem>> GetTasks()
    {
        var worker = await _db.GetAllTasks(1);
        return worker?.ContainerTasks ?? [];
    }

    #endregion
    #region POST Methods
    [HttpPost]
    public async Task<IActionResult> Create(string task, DateOnly date, int hours)
    {
        var worker = await _db.GetWorkers();
        if (worker.Count < 1) _ = await CreateWorker();
        TaskItem nuevaTarea = new()
        {
            Title = task,
            Deadline = date,
            Hours = hours,
            Priority = CalculatePriority(date, hours)
        };
        await CalculateSchedule(nuevaTarea);
        await _db.UpdateContainerTasks(1, nuevaTarea);
        return RedirectToAction("Index");
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

    private async Task<TaskItem> CalculateSchedule(TaskItem task)
    {
        Worker worker = await GetWorker();
        List<Schedule> horarios = worker.Schedules;
        List<TaskItem> tasksWorker = worker.ContainerTasks;
        if (tasksWorker.Count == 0)
        {
            var result = horarios[0].StartTime.AddHours(task.Hours);
            task.Start = DateTime.Today.Add(horarios[0].StartTime.ToTimeSpan());
            task.End = DateTime.Today.Add(result.ToTimeSpan());
            return task;
        }
        else
        {
            List<TaskItem> allTasks = [.. tasksWorker, task];
            allTasks.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            DateTime startTime = DateTime.Today.Add(horarios[0].StartTime.ToTimeSpan());

            foreach (var t in allTasks)
            {
                t.Start = startTime;
                t.End = startTime.AddHours(t.Hours);
                startTime = t.End;
            }
        }
        return task;
    }

    public async Task<Worker> GetWorker()
    {
        Worker worker = new()
        {
            Name = "Default Worker",
            Schedules =
            [
                new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(13, 0) },
                new() { StartTime = new TimeOnly(15, 0), EndTime = new TimeOnly(17, 0) }
            ]
        };
        return await _db.GetWorkerById(1) ?? worker;
    }

    public async Task<IActionResult> GetTasksCalendar()
    {
        var worker = await _db.GetAllTasks(1);
        var schedule = worker?.Schedules?.OrderBy(s => s.StartTime).ToList();
        var tasks = worker?.ContainerTasks?.OrderByDescending(t => t.Priority);

        if (schedule == null || tasks == null) return BadRequest("No se encontró el horario o las tareas.");

        int dayOffset = 0; // días desde hoy
        var currentStart = DateTime.Today.AddDays(dayOffset).Add(schedule[0].StartTime.ToTimeSpan());
        var currentEnd = DateTime.Today.AddDays(dayOffset).Add(schedule[0].EndTime.ToTimeSpan());
        int scheduleIndex = 0;

        List<TaskItem> result = new();

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

                    start = DateTime.Today.AddDays(dayOffset).Add(schedule[scheduleIndex].StartTime.ToTimeSpan());
                    currentEnd = DateTime.Today.AddDays(dayOffset).Add(schedule[scheduleIndex].EndTime.ToTimeSpan());
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
    
    private async Task<IActionResult> CreateWorker()
    {
        Worker worker = new()
        {
            Name = "Juan Perez",
            Schedules =
            [
                new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(13, 0) },
                new() { StartTime = new TimeOnly(15, 0), EndTime = new TimeOnly(17, 0) }
            ]
        };
        await _db.AddWorker(worker);
        return Ok();
    }
    
    #endregion
}
