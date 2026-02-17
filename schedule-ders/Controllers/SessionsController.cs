using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Admin")]
public class SessionsController : Controller
{
    private readonly ScheduleContext _context;

    public SessionsController(ScheduleContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? day, int? courseId)
    {
        var query = _context.Sessions
            .AsNoTracking()
            .Include(s => s.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Location.Contains(search) ||
                s.Time.Contains(search) ||
                s.Day.Contains(search) ||
                (s.Course != null && s.Course.CourseName.Contains(search)) ||
                (s.Course != null && s.Course.CourseSection.Contains(search)) ||
                (s.Course != null && s.Course.CourseLeader.Contains(search)));
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

        ViewData["CurrentSearch"] = search ?? string.Empty;
        ViewData["CurrentDay"] = day ?? string.Empty;
        ViewData["CurrentCourseId"] = courseId;

        ViewBag.DayOptions = await _context.Sessions
            .AsNoTracking()
            .Select(s => s.Day)
            .Distinct()
            .OrderBy(d => d)
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
        if (courseId.HasValue)
        {
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseID == courseId.Value);
            if (course is null)
            {
                return NotFound();
            }

            ViewData["CourseName"] = $"{course.CourseName} ({course.CourseSection})";
        }

        await PopulateCourseOptionsAsync(courseId);
        ViewData["ReturnUrl"] = returnUrl;
        return View(new Session { CourseID = courseId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SessionID,Day,Time,Location,CourseID")] Session session, string? returnUrl = null)
    {
        if (!await _context.Courses.AnyAsync(c => c.CourseID == session.CourseID))
        {
            ModelState.AddModelError(nameof(Session.CourseID), "Please select a valid course.");
        }

        if (!ModelState.IsValid)
        {
            var courseName = await _context.Courses
                .Where(c => c.CourseID == session.CourseID)
                .Select(c => $"{c.CourseName} ({c.CourseSection})")
                .FirstOrDefaultAsync();
            ViewData["CourseName"] = courseName ?? "Course";
            await PopulateCourseOptionsAsync(session.CourseID);
            ViewData["ReturnUrl"] = returnUrl;
            return View(session);
        }

        _context.Sessions.Add(session);
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

        ViewData["CourseName"] = session.Course is null
            ? "Course"
            : $"{session.Course.CourseName} ({session.Course.CourseSection})";
        ViewData["ReturnUrl"] = returnUrl;
        return View(session);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("SessionID,Day,Time,Location,CourseID")] Session session, string? returnUrl = null)
    {
        if (id != session.SessionID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var courseName = await _context.Courses
                .Where(c => c.CourseID == session.CourseID)
                .Select(c => $"{c.CourseName} ({c.CourseSection})")
                .FirstOrDefaultAsync();
            ViewData["CourseName"] = courseName ?? "Course";
            ViewData["ReturnUrl"] = returnUrl;
            return View(session);
        }

        try
        {
            _context.Update(session);
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
        if (id is null)
        {
            return NotFound();
        }

        var session = await _context.Sessions
            .AsNoTracking()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.SessionID == id);
        if (session is null)
        {
            return NotFound();
        }

        ViewData["CourseName"] = session.Course is null
            ? "Course"
            : $"{session.Course.CourseName} ({session.Course.CourseSection})";
        ViewData["ReturnUrl"] = returnUrl;
        return View(session);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl = null)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session is null)
        {
            return NotFound();
        }

        var courseId = session.CourseID;
        _context.Sessions.Remove(session);
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
            .Select(c => new { c.CourseID, Label = $"{c.CourseName} ({c.CourseSection}) - {c.CourseProfessor}" })
            .ToListAsync();

        ViewBag.CourseOptions = new SelectList(courses, "CourseID", "Label", selectedCourseId);
    }
}
