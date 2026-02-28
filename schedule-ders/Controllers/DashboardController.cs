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
            return RedirectToAction("Index", "Courses");
        }

        if (User.IsInRole("Professor"))
        {
            return RedirectToAction("Index", "ProfessorRequests");
        }

        if (User.IsInRole("Student"))
        {
            return RedirectToAction("Index", "StudentSchedule");
        }

        return View("NoRole");
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin()
    {
        await Task.CompletedTask;
        return RedirectToAction("Index", "Courses");
    }

    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> Professor()
    {
        await Task.CompletedTask;
        return RedirectToAction("Index", "ProfessorRequests");
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Student()
    {
        await Task.CompletedTask;
        return RedirectToAction("Index", "StudentSchedule");
    }
}
