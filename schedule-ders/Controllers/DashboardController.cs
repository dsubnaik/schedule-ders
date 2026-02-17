using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace schedule_ders.Controllers;

[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction(nameof(Admin));
        }

        if (User.IsInRole("Professor"))
        {
            return RedirectToAction(nameof(Professor));
        }

        if (User.IsInRole("Student"))
        {
            return RedirectToAction(nameof(Student));
        }

        return View("NoRole");
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return View();
    }

    [Authorize(Roles = "Professor")]
    public IActionResult Professor()
    {
        return View();
    }

    [Authorize(Roles = "Student")]
    public IActionResult Student()
    {
        return View();
    }
}
