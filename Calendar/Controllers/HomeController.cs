using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Calendar.Models;
using Calendar.Data;

namespace Calendar.Controllers;

public class HomeController(DB_Service db) : Controller
{
    private readonly DB_Service _db = db; 
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
    public IActionResult Login()
    {
        return View();
    }
    public IActionResult Settings()
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
            
        } else if(form.Action == "register") // Es un registro
        {
            var user_existe = await _db.GetUserByName(form.Name);
            if(user_existe != null)
            {
                ModelState.AddModelError(string.Empty, "El usuario ya existe.");
            }
            var user = new User
                {
                    Name = form.Name,
                    Password =  BCrypt.Net.BCrypt.HashPassword(form.Password),
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

    [HttpPost]
    public IActionResult Logout() 
    {
        Response.Cookies.Delete("UserId");
        return Ok( new { message = "Sesión cerrada correctamente" });
    }

    public async Task<IActionResult> GetUser()
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie))
        {
            ModelState.AddModelError("auth", "Se requiere autenticación.");
            return RedirectToAction("Login", "Home");
        }
        try
        {
            User user = await _db.GetUserById(Guid.Parse(userIdCookie)) ?? throw new KeyNotFoundException("Usuario no encontrado.");
            var breakTasks = user.ContainerTasks.OfType<BreakTask>().OrderBy(t => t.Start).ToList();
            return Ok(new
            {
                name = user.Name,
                schedule = user.Schedules.FirstOrDefault(),
                containerTasks = breakTasks
            });
        } catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUser([FromBody] User updateUser)
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie))
            return Unauthorized (new { message = "Se requiere autorización" });

        updateUser.Id = Guid.Parse(userIdCookie);

        try
        {
            await _db.UpdateUser(updateUser);
            return Ok();
        } catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSchedule([FromBody] Schedule schedule)
    {
        var userIdCookie = Request.Cookies["userId"];
        if(string.IsNullOrEmpty(userIdCookie)) return Unauthorized (new { message = "Se requiere autorización" });

        try
        {
            User user = await _db.GetUserById(Guid.Parse(userIdCookie)) ?? throw new KeyNotFoundException("Usuario no encontrado.");
            var userSchedule = user.Schedules.FirstOrDefault() ?? throw new KeyNotFoundException("Horario no encontrado.");
            userSchedule.StartTime = schedule.StartTime;
            userSchedule.EndTime = schedule.EndTime;

            await _db.UpdateUser(user);
            return Ok();
        } catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
}
