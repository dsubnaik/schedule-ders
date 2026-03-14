using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.Utilities;
using schedule_ders.ViewModels;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Admin")]
public class SemestersController : Controller
{
    private readonly ScheduleContext _context;

    public SemestersController(ScheduleContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = await BuildViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SemestersIndexViewModel input)
    {
        var normalizedCode = (input.NewSemesterCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            TempData["SemesterError"] = "Semester code is required.";
            return RedirectToAction(nameof(Index));
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedCode, @"^\d{6}$"))
        {
            TempData["SemesterError"] = "Use a 6-digit semester code like 202601 or 202609.";
            return RedirectToAction(nameof(Index));
        }

        var exists = await _context.Semesters
            .AsNoTracking()
            .AnyAsync(s => s.SemesterCode == normalizedCode);
        if (exists)
        {
            TempData["SemesterError"] = $"Semester '{normalizedCode}' already exists.";
            return RedirectToAction(nameof(Index));
        }

        _context.Semesters.Add(new Semester
        {
            SemesterCode = normalizedCode
        });
        await _context.SaveChangesAsync();
        TempData["SemesterMessage"] = $"Added {SemesterCodeFormatter.ToDisplayName(normalizedCode)}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var semester = await _context.Semesters
            .Include(s => s.Courses)
            .FirstOrDefaultAsync(s => s.SemesterId == id);

        if (semester is null)
        {
            return NotFound();
        }

        if (!SemesterCodeFormatter.IsPastSemester(semester.SemesterCode, DateTime.Today))
        {
            TempData["SemesterError"] = $"Can't delete {SemesterCodeFormatter.ToDisplayName(semester.SemesterCode)} until that semester is in the past.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var course in semester.Courses)
        {
            course.SemesterId = null;
        }

        _context.Semesters.Remove(semester);
        await _context.SaveChangesAsync();
        TempData["SemesterMessage"] = $"Deleted {SemesterCodeFormatter.ToDisplayName(semester.SemesterCode)}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetCurrent(int? semesterId, string? returnUrl)
    {
        if (semesterId.HasValue)
        {
            Response.Cookies.Append(
                SemesterContextHelper.AdminSemesterCookieKey,
                semesterId.Value.ToString(),
                new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
        }
        else
        {
            Response.Cookies.Delete(SemesterContextHelper.AdminSemesterCookieKey);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Courses");
    }

    private async Task<SemestersIndexViewModel> BuildViewModelAsync()
    {
        var today = DateTime.Today;
        var semesters = await _context.Semesters
            .AsNoTracking()
            .Include(s => s.Courses)
            .ThenInclude(c => c.Sessions)
            .OrderByDescending(s => s.SemesterCode)
            .ToListAsync();

        return new SemestersIndexViewModel
        {
            Semesters = semesters
                .Select(s => new SemesterListItemViewModel
                {
                    SemesterId = s.SemesterId,
                    SemesterCode = s.SemesterCode,
                    DisplayName = SemesterCodeFormatter.ToDisplayName(s.SemesterCode),
                    CourseCount = s.Courses.Count,
                    SessionCount = s.Courses
                        .SelectMany(c => c.Sessions.Select(session => new
                        {
                            c.CourseName,
                            c.CourseTitle,
                            c.CourseLeader,
                            session.Day,
                            session.Time,
                            session.Location
                        }))
                        .Distinct()
                        .Count(),
                    CanDelete = SemesterCodeFormatter.IsPastSemester(s.SemesterCode, today),
                    DeleteHint = SemesterCodeFormatter.IsPastSemester(s.SemesterCode, today)
                        ? "Delete"
                        : "Current or future semester"
                })
                .ToList()
        };
    }
}
