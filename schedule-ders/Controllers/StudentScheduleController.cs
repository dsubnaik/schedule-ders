using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.Services.Interfaces;
using schedule_ders.ViewModels;

namespace schedule_ders.Controllers;

public class StudentScheduleController : Controller
{
    private readonly ScheduleContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IScheduleQueryService _scheduleQueryService;

    public StudentScheduleController(
        ScheduleContext context,
        UserManager<IdentityUser> userManager,
        IScheduleQueryService scheduleQueryService)
    {
        _context = context;
        _userManager = userManager;
        _scheduleQueryService = scheduleQueryService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, string? day, string? professor)
    {
        var sessionSearch = await _scheduleQueryService.SearchSessionsAsync(
            search,
            day,
            professor,
            courseId: null,
            page: 1,
            pageSize: 500);

        var results = sessionSearch.Items
            .Select(s => new StudentScheduleRowViewModel
            {
                CourseID = s.CourseId,
                CourseName = s.CourseName,
                CourseSection = s.CourseSection,
                Professor = s.ProfessorName,
                Day = s.Day,
                Time = string.IsNullOrWhiteSpace(s.EndTime) ? s.StartTime : $"{s.StartTime}-{s.EndTime}",
                Location = s.Location,
                SILeader = s.SiLeaderName
            })
            .ToList();

        ViewData["DayOptions"] = await _context.Sessions
            .AsNoTracking()
            .Select(s => s.Day)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var vm = new StudentScheduleSearchViewModel
        {
            Search = search ?? string.Empty,
            Day = day ?? string.Empty,
            Professor = professor ?? string.Empty,
            Results = results
        };

        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Student"))
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                vm.CanManageFavorites = true;
                vm.FavoriteCourseIds = await _context.StudentFavoriteCourses
                    .AsNoTracking()
                    .Where(f => f.UserId == userId)
                    .Select(f => f.CourseID)
                    .ToHashSetAsync();
            }
        }

        return View(vm);
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Favorite(int courseId, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var courseExists = await _context.Courses.AnyAsync(c => c.CourseID == courseId);
        if (!courseExists)
        {
            return NotFound();
        }

        var exists = await _context.StudentFavoriteCourses
            .AnyAsync(f => f.UserId == userId && f.CourseID == courseId);

        if (!exists)
        {
            _context.StudentFavoriteCourses.Add(new StudentFavoriteCourse
            {
                UserId = userId,
                CourseID = courseId
            });
            await _context.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfavorite(int courseId, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var favorite = await _context.StudentFavoriteCourses
            .FirstOrDefaultAsync(f => f.UserId == userId && f.CourseID == courseId);

        if (favorite is not null)
        {
            _context.StudentFavoriteCourses.Remove(favorite);
            await _context.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
