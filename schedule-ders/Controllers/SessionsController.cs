using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.Utilities;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Admin")]
public class SessionsController : Controller
{
    private readonly ScheduleContext _context;

    public SessionsController(ScheduleContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? day, int? courseId, string? time, string? leader, string? location)
    {
        var searchValue = search?.Trim() ?? string.Empty;
        var timeValue = time?.Trim() ?? string.Empty;
        var leaderValue = leader?.Trim() ?? string.Empty;
        var locationValue = location?.Trim() ?? string.Empty;
        var compactTime = TimeSearchHelper.ToCompactToken(timeValue);
        var hasSearchTime = TimeSearchHelper.TryParseSearchTime(timeValue, out _);

        var query = _context.Sessions
            .AsNoTracking()
            .Include(s => s.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(s =>
                (s.Course != null && (s.Course.CourseName.Contains(searchValue) || s.Course.CourseTitle.Contains(searchValue))) ||
                (s.Course != null && s.Course.CourseSection.Contains(searchValue)));
        }

        if (!string.IsNullOrWhiteSpace(timeValue) && !hasSearchTime)
        {
            query = query.Where(s =>
                s.Time.Contains(timeValue) ||
                s.Time.Replace(":", "").Replace(".", "").Replace(" ", "").Replace("-", "").Contains(compactTime));
        }

        if (!string.IsNullOrWhiteSpace(leaderValue))
        {
            query = query.Where(s => s.Course != null && s.Course.CourseLeader.Contains(leaderValue));
        }

        if (!string.IsNullOrWhiteSpace(locationValue))
        {
            query = query.Where(s => s.Location.Contains(locationValue));
        }

        if (!string.IsNullOrWhiteSpace(day))
        {
            query = query.Where(s => s.Day == day);
        }

        if (courseId.HasValue)
        {
            query = query.Where(s => s.CourseID == courseId.Value);
        }

        var sessions = await query
            .OrderBy(s => s.Day)
            .ThenBy(s => s.Time)
            .ThenBy(s => s.Course!.CourseName)
            .ToListAsync();

        if (hasSearchTime)
        {
            sessions = sessions
                .Where(s =>
                    TimeSearchHelper.MatchesTimeRange(s.Time, timeValue) ||
                    TimeSearchHelper.MatchesTimeText(s.Time, timeValue))
                .ToList();
        }

        ViewData["CurrentSearch"] = searchValue;
        ViewData["CurrentTime"] = timeValue;
        ViewData["CurrentLeader"] = leaderValue;
        ViewData["CurrentLocation"] = locationValue;
        ViewData["CurrentDay"] = day ?? string.Empty;
        ViewData["CurrentCourseId"] = courseId;

        ViewBag.DayOptions = await _context.Sessions
            .AsNoTracking()
            .Select(s => s.Day)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        ViewBag.LeaderOptions = await _context.Courses
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseLeader))
            .Select(c => c.CourseLeader)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();

        ViewBag.LocationOptions = await _context.Sessions
            .AsNoTracking()
            .Where(s => !string.IsNullOrWhiteSpace(s.Location))
            .Select(s => s.Location)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();

        var courses = await _context.Courses
            .AsNoTracking()
            .OrderBy(c => c.CourseName)
            .ThenBy(c => c.CourseSection)
            .Select(c => new { c.CourseID, Label = $"{c.CourseName} ({c.CourseSection})" })
            .ToListAsync();

        ViewBag.CourseFilterOptions = new SelectList(courses, "CourseID", "Label", courseId);

        return View(sessions);
    }

    public async Task<IActionResult> Create(int? courseId, string? returnUrl = null)
    {
        string currentSiLeader = string.Empty;
        List<int> selectedCourseIds = [];
        if (courseId.HasValue)
        {
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseID == courseId.Value);
            if (course is null)
            {
                return NotFound();
            }

            ViewData["CourseName"] = $"{course.CourseName} ({course.CourseSection}) - {course.CourseTitle}";
            currentSiLeader = course.CourseLeader ?? string.Empty;
            selectedCourseIds.Add(course.CourseID);
        }

        ViewData["ShowSiLeaderField"] = true;
        ViewData["ShowSectionTargets"] = true;
        ViewData["CurrentSiLeader"] = currentSiLeader;
        ViewData["SelectedCourseIds"] = selectedCourseIds;
        await PopulateLeaderOptionsAsync();
        await PopulateCourseOptionsAsync(courseId);
        await PopulateSectionTargetOptionsAsync(courseId);
        ViewData["ReturnUrl"] = returnUrl;
        return View(new Session { CourseID = courseId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SessionID,Day,Time,Location,CourseID")] Session session, List<int>? sectionCourseIds, string? siLeader, string? returnUrl = null)
    {
        var baseCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == session.CourseID);
        if (baseCourse is null)
        {
            ModelState.AddModelError(nameof(Session.CourseID), "Please select a valid course.");
        }

        var selectedCourseIds = (sectionCourseIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (baseCourse is not null && selectedCourseIds.Count == 0)
        {
            selectedCourseIds.Add(baseCourse.CourseID);
        }

        if (baseCourse is not null)
        {
            var selectedCourses = await _context.Courses
                .AsNoTracking()
                .Where(c => selectedCourseIds.Contains(c.CourseID))
                .ToListAsync();

            var invalidSelectionExists = selectedCourses.Count != selectedCourseIds.Count ||
                                         selectedCourses.Any(c => c.CourseName != baseCourse.CourseName || c.CourseTitle != baseCourse.CourseTitle);
            if (invalidSelectionExists)
            {
                ModelState.AddModelError(nameof(Session.CourseID), "Selected sections must belong to the same course name.");
            }
        }

        if (!ModelState.IsValid)
        {
            var courseName = await _context.Courses
                .Where(c => c.CourseID == session.CourseID)
                .Select(c => $"{c.CourseName} ({c.CourseSection}) - {c.CourseTitle}")
                .FirstOrDefaultAsync();
            ViewData["CourseName"] = courseName ?? "Course";
            ViewData["ShowSiLeaderField"] = true;
            ViewData["ShowSectionTargets"] = true;
            ViewData["CurrentSiLeader"] = siLeader ?? string.Empty;
            ViewData["SelectedCourseIds"] = selectedCourseIds;
            await PopulateLeaderOptionsAsync();
            await PopulateCourseOptionsAsync(session.CourseID);
            await PopulateSectionTargetOptionsAsync(session.CourseID);
            ViewData["ReturnUrl"] = returnUrl;
            return View(session);
        }

        var existingSlotCourseIds = await _context.Sessions
            .AsNoTracking()
            .Where(s => selectedCourseIds.Contains(s.CourseID)
                        && s.Day == session.Day
                        && s.Time == session.Time
                        && s.Location == session.Location)
            .Select(s => s.CourseID)
            .Distinct()
            .ToListAsync();

        var missingCourseIds = selectedCourseIds
            .Except(existingSlotCourseIds)
            .ToList();

        var sessionsToCreate = missingCourseIds.Select(selectedCourseId => new Session
        {
            CourseID = selectedCourseId,
            Day = session.Day,
            Time = session.Time,
            Location = session.Location
        });

        _context.Sessions.AddRange(sessionsToCreate);

        if (!string.IsNullOrWhiteSpace(siLeader))
        {
            var coursesToUpdate = await _context.Courses
                .Where(c => selectedCourseIds.Contains(c.CourseID))
                .ToListAsync();
            foreach (var course in coursesToUpdate)
            {
                course.CourseLeader = siLeader.Trim();
            }
        }

        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id, string? returnUrl = null)
    {
        if (id is null)
        {
            return NotFound();
        }

        var session = await _context.Sessions
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.SessionID == id);
        if (session is null)
        {
            return NotFound();
        }

        var groupSessions = await _context.Sessions
            .Include(s => s.Course)
            .Where(s =>
                s.Course != null &&
                session.Course != null &&
                s.Course.CourseName == session.Course.CourseName &&
                s.Day == session.Day &&
                s.Time == session.Time &&
                s.Location == session.Location)
            .OrderBy(s => s.Course!.CourseSection)
            .ToListAsync();

        var selectedCourseIds = groupSessions
            .Select(s => s.CourseID)
            .Distinct()
            .ToList();

        var baseCourseId = selectedCourseIds.FirstOrDefault();
        var sessionIdsCsv = string.Join(",", groupSessions.Select(s => s.SessionID));

        ViewData["CourseName"] = session.Course is null
            ? "Course"
            : $"{session.Course.CourseName} ({session.Course.CourseSection}) - {session.Course.CourseTitle}";
        ViewData["SessionIdsCsv"] = sessionIdsCsv;
        ViewData["ShowCourseSelect"] = true;
        ViewData["ShowSiLeaderField"] = true;
        ViewData["ShowSectionTargets"] = true;
        ViewData["SelectedCourseIds"] = selectedCourseIds;
        ViewData["CurrentSiLeader"] = session.Course?.CourseLeader ?? string.Empty;
        await PopulateLeaderOptionsAsync();
        await PopulateCourseOptionsAsync(baseCourseId);
        await PopulateSectionTargetOptionsAsync(baseCourseId);
        ViewData["ReturnUrl"] = returnUrl;
        return View(session);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("SessionID,Day,Time,Location")] Session session, int? courseId, List<int>? sectionCourseIds, string? sessionIds, string? siLeader, string? returnUrl = null)
    {
        if (id != session.SessionID)
        {
            return NotFound();
        }

        var existingIds = ParseIds(sessionIds);
        if (existingIds.Count == 0)
        {
            existingIds.Add(id);
        }

        var existingSessions = await _context.Sessions
            .Include(s => s.Course)
            .Where(s => existingIds.Contains(s.SessionID))
            .ToListAsync();

        if (existingSessions.Count == 0)
        {
            return NotFound();
        }

        var baseCourseName = existingSessions
            .Select(s => s.Course?.CourseName)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty;
        var baseCourseTitle = existingSessions
            .Select(s => s.Course?.CourseTitle)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty;

        var selectedCourseIds = (sectionCourseIds ?? [])
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (selectedCourseIds.Count == 0)
        {
            selectedCourseIds = existingSessions
                .Select(s => s.CourseID)
                .Distinct()
                .ToList();
        }

        if (courseId.HasValue && !selectedCourseIds.Contains(courseId.Value))
        {
            selectedCourseIds.Insert(0, courseId.Value);
        }

        var selectedCourses = await _context.Courses
            .Where(c => selectedCourseIds.Contains(c.CourseID))
            .ToListAsync();

        if (selectedCourses.Count != selectedCourseIds.Count)
        {
            ModelState.AddModelError(nameof(Session.SessionID), "One or more selected sections are invalid.");
        }

        if (selectedCourses.Any(c => c.CourseName != baseCourseName || c.CourseTitle != baseCourseTitle))
        {
            ModelState.AddModelError(nameof(Session.SessionID), "Selected sections must belong to the same course.");
        }

        if (!ModelState.IsValid)
        {
            var fallbackCourseId = selectedCourseIds.FirstOrDefault();
            var fallbackCourseName = await _context.Courses
                .Where(c => c.CourseID == fallbackCourseId)
                .Select(c => $"{c.CourseName} ({c.CourseSection}) - {c.CourseTitle}")
                .FirstOrDefaultAsync();
            ViewData["CourseName"] = fallbackCourseName ?? "Course";
            ViewData["SessionIdsCsv"] = string.Join(",", existingIds);
            ViewData["ShowCourseSelect"] = true;
            ViewData["ShowSiLeaderField"] = true;
            ViewData["ShowSectionTargets"] = true;
            ViewData["SelectedCourseIds"] = selectedCourseIds;
            ViewData["CurrentSiLeader"] = siLeader ?? string.Empty;
            await PopulateLeaderOptionsAsync();
            await PopulateCourseOptionsAsync(fallbackCourseId);
            await PopulateSectionTargetOptionsAsync(fallbackCourseId);
            ViewData["ReturnUrl"] = returnUrl;
            return View(session);
        }

        try
        {
            var existingByCourseId = existingSessions.ToDictionary(s => s.CourseID, s => s);

            foreach (var selectedCourseId in selectedCourseIds)
            {
                if (existingByCourseId.TryGetValue(selectedCourseId, out var existing))
                {
                    existing.Day = session.Day;
                    existing.Time = session.Time;
                    existing.Location = session.Location;
                }
                else
                {
                    var duplicateExists = await _context.Sessions
                .AnyAsync(s => s.CourseID == selectedCourseId
                                       && s.Day == session.Day
                                       && s.Time == session.Time
                                       && s.Location == session.Location);

                    if (!duplicateExists)
                    {
                        _context.Sessions.Add(new Session
                        {
                            CourseID = selectedCourseId,
                            Day = session.Day,
                            Time = session.Time,
                            Location = session.Location
                        });
                    }
                }
            }

            var toRemove = existingSessions
                .Where(s => !selectedCourseIds.Contains(s.CourseID))
                .ToList();
            if (toRemove.Count > 0)
            {
                _context.Sessions.RemoveRange(toRemove);
            }

            if (!string.IsNullOrWhiteSpace(siLeader))
            {
                foreach (var course in selectedCourses)
                {
                    course.CourseLeader = siLeader.Trim();
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SessionExists(session.SessionID))
            {
                return NotFound();
            }

            throw;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id, string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, string? sessionIds, string? returnUrl = null)
    {
        var ids = ParseIds(sessionIds);
        if (ids.Count == 0 && id > 0)
        {
            ids.Add(id);
        }

        var sessions = await _context.Sessions
            .Where(s => ids.Contains(s.SessionID))
            .ToListAsync();
        if (sessions.Count == 0)
        {
            return NotFound();
        }

        _context.Sessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private bool SessionExists(int id)
    {
        return _context.Sessions.Any(e => e.SessionID == id);
    }

    private async Task PopulateCourseOptionsAsync(int? selectedCourseId = null)
    {
        var courses = await _context.Courses
            .AsNoTracking()
            .OrderBy(c => c.CourseName)
            .ThenBy(c => c.CourseSection)
            .Select(c => new { c.CourseID, Label = $"{c.CourseName} - {c.CourseTitle} ({c.CourseSection}) - {c.CourseProfessor}" })
            .ToListAsync();

        ViewBag.CourseOptions = new SelectList(courses, "CourseID", "Label", selectedCourseId);
    }

    private async Task PopulateLeaderOptionsAsync()
    {
        ViewBag.LeaderOptions = await _context.Courses
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseLeader))
            .Select(c => c.CourseLeader)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> SectionTargets(int? courseId)
    {
        if (!courseId.HasValue)
        {
            return Json(Array.Empty<object>());
        }

        var baseCourse = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CourseID == courseId.Value);

        if (baseCourse is null)
        {
            return Json(Array.Empty<object>());
        }

        var options = await _context.Courses
            .AsNoTracking()
            .Where(c => c.CourseName == baseCourse.CourseName)
            .Where(c => c.CourseTitle == baseCourse.CourseTitle)
            .OrderBy(c => c.CourseSection)
            .Select(c => new
            {
                courseId = c.CourseID,
                section = c.CourseSection
            })
            .ToListAsync();

        return Json(options);
    }

    private async Task PopulateSectionTargetOptionsAsync(int? courseId)
    {
        if (!courseId.HasValue)
        {
            ViewBag.SectionTargetOptions = new List<SelectListItem>();
            return;
        }

        var baseCourse = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CourseID == courseId.Value);

        if (baseCourse is null)
        {
            ViewBag.SectionTargetOptions = new List<SelectListItem>();
            return;
        }

        ViewBag.SectionTargetOptions = await _context.Courses
            .AsNoTracking()
            .Where(c => c.CourseName == baseCourse.CourseName)
            .Where(c => c.CourseTitle == baseCourse.CourseTitle)
            .OrderBy(c => c.CourseSection)
            .Select(c => new SelectListItem
            {
                Value = c.CourseID.ToString(),
                Text = c.CourseSection
            })
            .ToListAsync();
    }

    private static List<int> ParseIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }
}
