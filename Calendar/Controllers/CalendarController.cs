using Microsoft.AspNetCore.Mvc;

namespace Calendar.Controllers;

public class CalendarController : Controller
{
    #region GET Methods
    [HttpGet]
    public IActionResult Index()
    {
        if(string.IsNullOrEmpty(Request.Cookies["userId"])) return RedirectToAction("Login", "Home");
        return View();
    }
    #endregion
}