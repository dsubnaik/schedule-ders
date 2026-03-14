using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    public async Task<IActionResult> Index(string? crn, string? course, string? time, string? professor, string? day, int? semesterId)
    {
        if (!semesterId.HasValue && SemesterContextHelper.ReadSelectedSemesterId(Request) is int cookieSemesterId)
        {
            semesterId = cookieSemesterId;
        }

        var crnValue = crn?.Trim() ?? string.Empty;
        var courseValue = course?.Trim() ?? string.Empty;
        var timeValue = time?.Trim() ?? string.Empty;
        var professorValue = professor?.Trim() ?? string.Empty;
        var dayValue = day?.Trim() ?? string.Empty;
        var compactTime = TimeSearchHelper.ToCompactToken(timeValue);
        var hasSearchTime = TimeSearchHelper.TryParseSearchTime(timeValue, out _);

        var query = _context.Courses
            .AsNoTracking()
            .Include(c => c.Semester)
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

        if (semesterId.HasValue)
        {
            query = query.Where(c => c.SemesterId == semesterId.Value);
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
        ViewData["CurrentSemesterId"] = semesterId;

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

        await PopulateSemesterOptionsAsync(semesterId);

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
            .Include(c => c.Semester)
            .Include(c => c.Sessions.OrderBy(s => s.Day).ThenBy(s => s.Time))
            .FirstOrDefaultAsync(m => m.CourseID == id);

        if (course is null)
        {
            return NotFound();
        }

        return View(course);
    }

    public async Task<IActionResult> Create(int? semesterId = null)
    {
        PopulateCourseNameOptions();
        PopulateProfessorOptions();
        PopulateLeaderOptions();
        await PopulateSemesterOptionsAsync(semesterId);
        return View(new Course
        {
            SemesterId = semesterId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CourseID,CourseCrn,CourseName,CourseTitle,CourseSection,CourseMeetingDays,CourseMeetingTime,CourseProfessor,CourseLeader,OfficeHoursDay,OfficeHoursTime,OfficeHoursLocation,SemesterId")] Course course)
    {
        if (!ModelState.IsValid)
        {
            PopulateCourseNameOptions();
            PopulateProfessorOptions();
            PopulateLeaderOptions();
            await PopulateSemesterOptionsAsync(course.SemesterId);
            return View(course);
        }

        await NormalizeLeaderFromDirectoryAsync(course);
        await SyncLeaderDirectoryEntryAsync(course);
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

        PopulateCourseNameOptions();
        PopulateProfessorOptions();
        PopulateLeaderOptions();
        await PopulateSemesterOptionsAsync(course.SemesterId);
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("CourseID,CourseCrn,CourseName,CourseTitle,CourseSection,CourseMeetingDays,CourseMeetingTime,CourseProfessor,CourseLeader,OfficeHoursDay,OfficeHoursTime,OfficeHoursLocation,SemesterId")] Course course)
    {
        if (id != course.CourseID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            PopulateCourseNameOptions();
            PopulateProfessorOptions();
            PopulateLeaderOptions();
            await PopulateSemesterOptionsAsync(course.SemesterId);
            return View(course);
        }

        try
        {
            await NormalizeLeaderFromDirectoryAsync(course);
            await SyncLeaderDirectoryEntryAsync(course);
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
    public async Task<IActionResult> LookupLeaderByCourseName(string? courseName, string? courseSection, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(courseName))
        {
            return BadRequest();
        }

        var normalizedName = courseName.Trim();
        var normalizedSection = (courseSection ?? string.Empty).Trim();
        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.CourseName == normalizedName && !string.IsNullOrWhiteSpace(c.CourseLeader));

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.CourseID != excludeId.Value);
        }

        string? leader;
        if (!string.IsNullOrWhiteSpace(normalizedSection))
        {
            leader = await query
                .Where(c => c.CourseSection == normalizedSection)
                .Select(c => c.CourseLeader)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(leader))
            {
                return Json(new { courseLeader = leader });
            }
        }

        leader = await query
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
            var siLeaderMatch = await _context.SILeaders
                .AsNoTracking()
                .Where(l => !string.IsNullOrWhiteSpace(l.StoredCourseAssignments))
                .Select(l => new
                {
                    l.LeaderName,
                    l.StoredCourseAssignments
                })
                .ToListAsync();

            var exactMatch = siLeaderMatch.FirstOrDefault(l =>
                ParseAssignments(l.StoredCourseAssignments).Any(a =>
                    string.Equals(a.CourseName, normalizedName, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(normalizedSection)
                    && string.Equals(a.CourseSection, normalizedSection, StringComparison.OrdinalIgnoreCase)));

            var fallbackMatch = siLeaderMatch.FirstOrDefault(l =>
                ParseAssignments(l.StoredCourseAssignments).Any(a =>
                    string.Equals(a.CourseName, normalizedName, StringComparison.OrdinalIgnoreCase)));

            var matchedLeader = exactMatch?.LeaderName ?? fallbackMatch?.LeaderName;
            if (string.IsNullOrWhiteSpace(matchedLeader))
            {
                return NotFound();
            }

            return Json(new { courseLeader = GetDisplayLeaderName(matchedLeader) });
        }

        return Json(new { courseLeader = GetDisplayLeaderName(leader) });
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

    private void PopulateCourseNameOptions()
    {
        var fromCourses = _context.Courses
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseName))
            .Select(c => c.CourseName.Trim())
            .ToList();

        var fromCatalog = _context.CourseCatalogEntries
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseName))
            .Select(c => c.CourseName.Trim())
            .ToList();

        var fromLeaderAssignments = _context.SILeaders
            .AsNoTracking()
            .Where(l => !string.IsNullOrWhiteSpace(l.StoredCourseAssignments))
            .Select(l => l.StoredCourseAssignments)
            .ToList()
            .SelectMany(ParseAssignments)
            .Select(a => a.CourseName.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        ViewBag.CourseNameOptions = fromCourses
            .Concat(fromCatalog)
            .Concat(fromLeaderAssignments)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private void PopulateLeaderOptions()
    {
        var fromCourses = _context.Courses
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseLeader))
            .Select(c => c.CourseLeader.Trim())
            .ToList();

        var fromDirectory = _context.SILeaders
            .AsNoTracking()
            .Where(l => !string.IsNullOrWhiteSpace(l.LeaderName))
            .Select(l => l.LeaderName.Trim())
            .ToList();

        ViewBag.LeaderOptions = fromCourses
            .Concat(fromDirectory)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private async Task PopulateSemesterOptionsAsync(int? selectedSemesterId = null)
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

        ViewBag.SemesterOptions = new SelectList(semesters, "SemesterId", "Label", selectedSemesterId);
    }

    private async Task NormalizeLeaderFromDirectoryAsync(Course course)
    {
        if (string.IsNullOrWhiteSpace(course.CourseLeader))
        {
            return;
        }

        var normalized = course.CourseLeader.Trim();
        var existing = await _context.SILeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LeaderName.ToLower() == normalized.ToLower());

        if (existing is not null)
        {
            course.CourseLeader = existing.LeaderName;
            return;
        }

        var firstNameMatches = await _context.SILeaders
            .AsNoTracking()
            .Where(l => !string.IsNullOrWhiteSpace(l.LeaderName))
            .Select(l => l.LeaderName)
            .ToListAsync();

        var firstNameMatch = firstNameMatches
            .Where(name => string.Equals(GetDisplayLeaderName(name), normalized, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (firstNameMatch.Count == 1)
        {
            course.CourseLeader = firstNameMatch[0];
        }
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

    private static List<(string CourseName, string CourseSection)> ParseAssignments(string? rawAssignments)
    {
        if (string.IsNullOrWhiteSpace(rawAssignments))
        {
            return [];
        }

        return rawAssignments
            .Replace("\r", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length < 2)
                {
                    return (CourseName: string.Empty, CourseSection: string.Empty);
                }

                return (CourseName: parts[0], CourseSection: parts[1]);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.CourseName) && !string.IsNullOrWhiteSpace(x.CourseSection))
            .Distinct()
            .ToList();
    }

    private static string GetDisplayLeaderName(string leaderName)
    {
        var normalized = (leaderName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var firstSpace = normalized.IndexOf(' ');
        return firstSpace > 0 ? normalized[..firstSpace] : normalized;
    }

    private async Task SyncLeaderDirectoryEntryAsync(Course course)
    {
        var leaderName = (course.CourseLeader ?? string.Empty).Trim();
        var courseName = (course.CourseName ?? string.Empty).Trim();
        var courseSection = (course.CourseSection ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(leaderName) || string.IsNullOrWhiteSpace(courseName) || string.IsNullOrWhiteSpace(courseSection))
        {
            return;
        }

        var existing = await _context.SILeaders
            .FirstOrDefaultAsync(l => l.LeaderName.ToLower() == leaderName.ToLower());

        if (existing is null)
        {
            _context.SILeaders.Add(new SILeader
            {
                ANumber = GeneratePlaceholderANumber(),
                LeaderName = leaderName,
                StoredCourseAssignments = $"{courseName}|{courseSection}"
            });
            return;
        }

        existing.StoredCourseAssignments = MergeAssignments(existing.StoredCourseAssignments, courseName, courseSection);
    }

    private static string MergeAssignments(string? existingAssignments, string courseName, string courseSection)
    {
        var combined = ParseAssignments(existingAssignments)
            .Append((courseName.Trim(), courseSection.Trim()))
            .Distinct()
            .Select(x => $"{x.Item1}|{x.Item2}")
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        return string.Join(Environment.NewLine, combined);
    }

    private static string GeneratePlaceholderANumber()
    {
        return $"TMP{Guid.NewGuid():N}"[..11].ToUpperInvariant();
    }
}

