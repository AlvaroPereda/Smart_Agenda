using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Calendar.Models;
using Calendar.Data;

namespace Calendar.Controllers;

public class HomeController(DB_Service db) : Controller
{

    #region Private Logic
    private readonly DB_Service _db = db;

    public class AuthForm
    {
        public required string Name { get; set; }
        public required string Password { get; set; }
        public required string Action { get; set; }
        public TimeOnly? Start { get; set; }
        public TimeOnly? End { get; set; }
    }

    #endregion
    #region GET Methods
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Settings()
    {
        if(string.IsNullOrEmpty(Request.Cookies["userId"])) return RedirectToAction("Login", "Home");
        return View();
    }
    #endregion
    #region POST Methods
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
                ModelState.AddModelError(string.Empty, "El usuario existe inicia sesión.");
                return View();
            }

            if(form.Start == null || form.End == null)
            {
                ModelState.AddModelError(string.Empty, "Debe especificar un horario válido.");
                return View();
            }

            var user = new User
                {
                    Name = form.Name,
                    Password =  BCrypt.Net.BCrypt.HashPassword(form.Password),
                    Schedule =
                    new Schedule
                    {
                        StartTime = form.Start.Value,
                        EndTime = form.End.Value,
                    },
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
        if(string.IsNullOrEmpty(userIdCookie)) return Unauthorized(new { message = "Usuario no encontrado." });
        try
        {
            User user = await _db.GetUserById(Guid.Parse(userIdCookie));
            if(user == null) return Unauthorized(new { message = "Usuario no encontrado." });
            
            var breakTasks = user.ContainerTasks.OfType<BreakTask>().OrderBy(t => t.Start).ToList();
            return Ok(new
            {
                name = user.Name,
                schedule = user.Schedule,
                containerTasks = breakTasks
            });
        } catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
    #endregion
    #region PUT Methods
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
            var userSchedule = user.Schedule ?? throw new KeyNotFoundException("Horario no encontrado.");
            userSchedule.StartTime = schedule.StartTime;
            userSchedule.EndTime = schedule.EndTime;

            await _db.UpdateUser(user);
            return Ok();
        } catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }   
    #endregion
}
