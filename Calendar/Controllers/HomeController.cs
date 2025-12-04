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
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, bool login, string? start, string? end)
    {
        if(login) // Es un inicio de sesión
        {
            var user = await _db.AuthenticateUser(username, password);
            if(user != null)
            {
                Response.Cookies.Append("UserId", user.Id.ToString());
                return RedirectToAction("Index", "Calendar");
            }
        }
        else // Es un registro
        {
            var user = await _db.GetUserByName(username);
            if(user == null)
            {
                var wser = new User
                {
                    Name = username,
                    Password = password,
                    Schedules =
                    [
                        new Schedule
                        {
                            StartTime = TimeOnly.Parse(start!),
                            EndTime = TimeOnly.Parse(end!)
                        }
                    ],
                    ContainerTasks = []
                };
                await _db.AddUser(wser);
                Console.WriteLine(wser.Id);
                Response.Cookies.Append("UserId", wser.Id.ToString());
                return RedirectToAction("Index", "Calendar");
            }
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
}
