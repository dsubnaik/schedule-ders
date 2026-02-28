using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.Utilities;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Admin")]
public class CoursesController : Controller
{
    private readonly ScheduleContext _context;

    public CoursesController(ScheduleContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? crn, string? course, string? time, string? professor, string? day)
    {
        var crnValue = crn?.Trim() ?? string.Empty;
        var courseValue = course?.Trim() ?? string.Empty;
        var timeValue = time?.Trim() ?? string.Empty;
        var professorValue = professor?.Trim() ?? string.Empty;
        var dayValue = day?.Trim() ?? string.Empty;
        var compactTime = TimeSearchHelper.ToCompactToken(timeValue);
        var hasSearchTime = TimeSearchHelper.TryParseSearchTime(timeValue, out _);

        var query = _context.Courses
            .AsNoTracking()
            .Include(c => c.Sessions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(crnValue))
        {
            query = query.Where(c => c.CourseCrn.Contains(crnValue));
        }

        if (!string.IsNullOrWhiteSpace(courseValue))
        {
            query = query.Where(c => c.CourseName.Contains(courseValue) || c.CourseTitle.Contains(courseValue));
        }

        if (!string.IsNullOrWhiteSpace(professorValue))
        {
            query = query.Where(c => c.CourseProfessor.Contains(professorValue));
        }

        if (!string.IsNullOrWhiteSpace(dayValue))
        {
            query = query.Where(c => c.CourseMeetingDays.Contains(dayValue));
        }

        if (!string.IsNullOrWhiteSpace(timeValue) && !hasSearchTime)
        {
            query = query.Where(c =>
                c.CourseMeetingTime.Contains(timeValue) ||
                c.CourseMeetingTime.Replace(":", "").Replace(".", "").Replace(" ", "").Replace("-", "").Contains(compactTime));
        }

        var courses = await query
            .OrderBy(c => c.CourseName)
            .ThenBy(c => c.CourseSection)
            .ToListAsync();

        if (hasSearchTime)
        {
            courses = courses
                .Where(c =>
                    TimeSearchHelper.MatchesTimeRange(c.CourseMeetingTime, timeValue) ||
                    TimeSearchHelper.MatchesTimeText(c.CourseMeetingTime, timeValue))
                .ToList();
        }

        ViewData["CurrentCrn"] = crnValue;
        ViewData["CurrentCourse"] = courseValue;
        ViewData["CurrentTime"] = timeValue;
        ViewData["CurrentProfessor"] = professorValue;
        ViewData["CurrentDay"] = dayValue;

        ViewBag.ProfessorOptions = await _context.Courses
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseProfessor))
            .Select(c => c.CourseProfessor)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        ViewBag.DayOptions = await _context.Courses
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseMeetingDays))
            .Select(c => c.CourseMeetingDays)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var notificationCookieKey = "sd-admin-last-seen-requests";
        var lastSeenRaw = Request.Cookies[notificationCookieKey];
        if (long.TryParse(lastSeenRaw, out var lastSeenUnix))
        {
            var lastSeenUtc = DateTimeOffset.FromUnixTimeSeconds(lastSeenUnix).UtcDateTime;
            var newRequests = await _context.SIRequests
                .AsNoTracking()
                .CountAsync(r => r.SubmittedAtUtc > lastSeenUtc);

            if (newRequests > 0)
            {
                ViewData["AdminNotification"] = newRequests == 1
                    ? "1 new SI request was submitted."
                    : $"{newRequests} new SI requests were submitted.";
            }
        }

        Response.Cookies.Append(
            notificationCookieKey,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

        return View(courses);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .Include(c => c.Sessions.OrderBy(s => s.Day).ThenBy(s => s.Time))
            .FirstOrDefaultAsync(m => m.CourseID == id);

        if (course is null)
        {
            return NotFound();
        }

        return View(course);
    }

    public IActionResult Create()
    {
        PopulateProfessorOptions();
        return View(new Course());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CourseID,CourseCrn,CourseName,CourseTitle,CourseSection,CourseMeetingDays,CourseMeetingTime,CourseProfessor,CourseLeader,OfficeHoursDay,OfficeHoursTime,OfficeHoursLocation")] Course course)
    {
        if (!ModelState.IsValid)
        {
            PopulateProfessorOptions();
            return View(course);
        }

        _context.Add(course);
        await UpsertCatalogEntryAsync(course.CourseCrn, course.CourseName, course.CourseTitle, course.CourseSection);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var course = await _context.Courses.FindAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        PopulateProfessorOptions();
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("CourseID,CourseCrn,CourseName,CourseTitle,CourseSection,CourseMeetingDays,CourseMeetingTime,CourseProfessor,CourseLeader,OfficeHoursDay,OfficeHoursTime,OfficeHoursLocation")] Course course)
    {
        if (id != course.CourseID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            PopulateProfessorOptions();
            return View(course);
        }

        try
        {
            _context.Update(course);
            await UpsertCatalogEntryAsync(course.CourseCrn, course.CourseName, course.CourseTitle, course.CourseSection);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CourseExists(course.CourseID))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> LookupByCrn(string? crn)
    {
        if (string.IsNullOrWhiteSpace(crn))
        {
            return BadRequest();
        }

        var query = _context.CourseCatalogEntries
            .AsNoTracking()
            .Where(c => c.CourseCrn == crn.Trim());

        var course = await query
            .Select(c => new
            {
                c.CourseName,
                c.CourseTitle,
                c.CourseSection
            })
            .FirstOrDefaultAsync();

        if (course is null)
        {
            return NotFound();
        }

        return Json(course);
    }

    [HttpGet]
    public async Task<IActionResult> LookupLeaderByCourseName(string? courseName, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(courseName))
        {
            return BadRequest();
        }

        var normalizedName = courseName.Trim();
        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.CourseName == normalizedName && !string.IsNullOrWhiteSpace(c.CourseLeader));

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.CourseID != excludeId.Value);
        }

        var leader = await query
            .GroupBy(c => c.CourseLeader)
            .Select(g => new
            {
                Leader = g.Key!,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Leader)
            .Select(x => x.Leader)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(leader))
        {
            return NotFound();
        }

        return Json(new { courseLeader = leader });
    }

    public async Task<IActionResult> Delete(int? id)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course is not null)
        {
            var linkedRequests = await _context.SIRequests
                .Where(r => r.CourseID == id)
                .ToListAsync();

            foreach (var request in linkedRequests)
            {
                request.CourseID = null;

                if (string.IsNullOrWhiteSpace(request.RequestedCourseName))
                {
                    request.RequestedCourseName = course.CourseName;
                }

                if (string.IsNullOrWhiteSpace(request.RequestedCourseTitle))
                {
                    request.RequestedCourseTitle = course.CourseTitle;
                }

                if (string.IsNullOrWhiteSpace(request.RequestedCourseSection))
                {
                    request.RequestedCourseSection = course.CourseSection;
                }

                if (string.IsNullOrWhiteSpace(request.RequestedCourseProfessor))
                {
                    request.RequestedCourseProfessor = course.CourseProfessor;
                }
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool CourseExists(int id)
    {
        return _context.Courses.Any(e => e.CourseID == id);
    }

    private void PopulateProfessorOptions()
    {
        ViewBag.ProfessorOptions = _context.Courses
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseProfessor))
            .Select(c => c.CourseProfessor)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
    }

    private async Task UpsertCatalogEntryAsync(string crn, string courseName, string courseTitle, string courseSection)
    {
        var normalizedCrn = crn.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCrn))
        {
            return;
        }

        var entry = await _context.CourseCatalogEntries.FirstOrDefaultAsync(c => c.CourseCrn == normalizedCrn);
        if (entry is null)
        {
            _context.CourseCatalogEntries.Add(new CourseCatalogEntry
            {
                CourseCrn = normalizedCrn,
                CourseName = courseName.Trim(),
                CourseTitle = courseTitle.Trim(),
                CourseSection = courseSection.Trim()
            });
            return;
        }

        entry.CourseName = courseName.Trim();
        entry.CourseTitle = courseTitle.Trim();
        entry.CourseSection = courseSection.Trim();
    }
}

