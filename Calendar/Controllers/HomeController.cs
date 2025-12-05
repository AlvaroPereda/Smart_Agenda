using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Calendar.Models;
using Calendar.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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

    public class AuthForm
    {
        public required string Name { get; set; }
        public required string Password { get; set; }
        public required string Action { get; set; }
        public string? Start { get; set; }
        public string? End { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromForm] AuthForm form)
    {
        if(!ModelState.IsValid)
            return View(form);

        if(form.Action == "login") // Es un inicio de sesión
        {
            var user = await _db.AuthenticateUser(form.Name, form.Password);
            if(user == null)
            {
                ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña incorrectos.");
                return View();
            }

            Response.Cookies.Append("UserId", user.Id.ToString());
            return RedirectToAction("Index", "Calendar");
        } else if(form.Action == "register")
        {
            var user_existe = await _db.GetUserByName(form.Name);
            if(user_existe != null)
            {
                ModelState.AddModelError(string.Empty, "El usuario ya existe.");
            }
            string passwordhash = BCrypt.Net.BCrypt.HashPassword(form.Password);
            var user = new User
                {
                    Name = form.Name,
                    Password = passwordhash,
                    Schedules =
                    [
                        new Schedule
                        {
                            StartTime = TimeOnly.Parse(form.Start!),
                            EndTime = TimeOnly.Parse(form.End!)
                        }
                    ],
                    ContainerTasks = []
                };
                await _db.AddUser(user);
                Response.Cookies.Append("UserId", user.Id.ToString());
                return RedirectToAction("Index", "Calendar");
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
}
