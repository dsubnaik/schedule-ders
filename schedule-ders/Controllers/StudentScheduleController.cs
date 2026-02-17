using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.ViewModels;

namespace schedule_ders.Controllers;

public class StudentScheduleController : Controller
{
    private readonly ScheduleContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public StudentScheduleController(ScheduleContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, string? day, string? professor)
    {
        var query = _context.Sessions
            .AsNoTracking()
            .Include(s => s.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                (s.Course != null && s.Course.CourseName.Contains(search)) ||
                (s.Course != null && s.Course.CourseSection.Contains(search)) ||
                (s.Course != null && s.Course.CourseLeader.Contains(search)) ||
                s.Day.Contains(search) ||
                s.Time.Contains(search) ||
                s.Location.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(day))
        {
            query = query.Where(s => s.Day == day);
        }

        if (!string.IsNullOrWhiteSpace(professor))
        {
            query = query.Where(s => s.Course != null && s.Course.CourseProfessor.Contains(professor));
        }

        var results = await query
            .OrderBy(s => s.Day)
            .ThenBy(s => s.Time)
            .Select(s => new StudentScheduleRowViewModel
            {
                CourseID = s.CourseID,
                CourseName = s.Course != null ? s.Course.CourseName : string.Empty,
                CourseSection = s.Course != null ? s.Course.CourseSection : string.Empty,
                Professor = s.Course != null ? s.Course.CourseProfessor : string.Empty,
                Day = s.Day,
                Time = s.Time,
                Location = s.Location,
                SILeader = s.Course != null ? s.Course.CourseLeader : string.Empty
            })
            .ToListAsync();

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
