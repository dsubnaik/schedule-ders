using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.Services.Interfaces;
using schedule_ders.Utilities;
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
    public async Task<IActionResult> Index(string? search, string? time, string? day, string? professor, int? semesterId)
    {
        var sessionSearch = await _scheduleQueryService.SearchSessionsAsync(
            search,
            time,
            day,
            professor,
            semesterId,
            courseId: null,
            page: 1,
            pageSize: 500);

        var results = sessionSearch.Items
            .Select(s => new StudentScheduleRowViewModel
            {
                CourseID = s.CourseId,
                CourseName = s.CourseName,
                CourseTitle = s.CourseTitle,
                CourseSection = s.CourseSection,
                Professor = s.ProfessorName,
                Day = s.Day,
                Time = string.IsNullOrWhiteSpace(s.EndTime) ? s.StartTime : $"{s.StartTime}-{s.EndTime}",
                Location = s.Location,
                SILeader = s.SiLeaderName
            })
            .ToList();

        var dayOptions = await _context.Sessions
            .AsNoTracking()
            .Select(s => s.Day)
            .Distinct()
            .ToListAsync();
        ViewData["DayOptions"] = dayOptions
            .OrderBy(GetDaySortOrder)
            .ThenBy(d => d)
            .ToList();
        await PopulateSemesterOptionsAsync(semesterId);

        var vm = new StudentScheduleSearchViewModel
        {
            Search = search ?? string.Empty,
            Time = time ?? string.Empty,
            Day = day ?? string.Empty,
            Professor = professor ?? string.Empty,
            SemesterId = semesterId,
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
    public async Task<IActionResult> FavoriteGroup(List<int>? courseIds, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var normalizedIds = (courseIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var validIds = await _context.Courses
            .AsNoTracking()
            .Where(c => normalizedIds.Contains(c.CourseID))
            .Select(c => c.CourseID)
            .ToListAsync();

        if (validIds.Count != normalizedIds.Count)
        {
            return NotFound();
        }

        var existingIds = await _context.StudentFavoriteCourses
            .Where(f => f.UserId == userId && normalizedIds.Contains(f.CourseID))
            .Select(f => f.CourseID)
            .ToListAsync();

        var toAdd = normalizedIds.Except(existingIds).ToList();
        if (toAdd.Count > 0)
        {
            _context.StudentFavoriteCourses.AddRange(toAdd.Select(id => new StudentFavoriteCourse
            {
                UserId = userId,
                CourseID = id
            }));
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

    [Authorize(Roles = "Student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnfavoriteGroup(List<int>? courseIds, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var normalizedIds = (courseIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var favorites = await _context.StudentFavoriteCourses
            .Where(f => f.UserId == userId && normalizedIds.Contains(f.CourseID))
            .ToListAsync();

        if (favorites.Count > 0)
        {
            _context.StudentFavoriteCourses.RemoveRange(favorites);
            await _context.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private static int GetDaySortOrder(string? day) => day?.Trim().ToLowerInvariant() switch
    {
        "monday" => 1,
        "tuesday" => 2,
        "wednesday" => 3,
        "thursday" => 4,
        "friday" => 5,
        "saturday" => 6,
        "sunday" => 7,
        _ => 99
    };

    private async Task PopulateSemesterOptionsAsync(int? selectedSemesterId)
    {
        var semesters = await _context.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.SemesterCode)
            .Select(s => new
            {
                s.SemesterId,
                Label = $"{SemesterCodeFormatter.ToDisplayName(s.SemesterCode)} ({s.SemesterCode})"
            })
            .ToListAsync();

        ViewData["SemesterOptions"] = new SelectList(semesters, "SemesterId", "Label", selectedSemesterId);
    }
}
