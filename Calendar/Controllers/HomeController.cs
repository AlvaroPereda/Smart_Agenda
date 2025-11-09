using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Calendar.Models;
using Calendar.Data;

namespace Calendar.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DB_Service _db;

    public HomeController(ILogger<HomeController> logger, DB_Service db)
    {
        _logger = logger;
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
    public IActionResult Privacy()
    {
        return View();
    }


    public async Task<IActionResult> CreateWorker()
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
    public async Task<List<Worker>> GetWorkers()
    {
        List<Worker> workers = await _db.GetWorkers();
        return workers;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
}
