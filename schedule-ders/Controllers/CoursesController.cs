using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Admin")]
public class CoursesController : Controller
{
    private readonly ScheduleContext _context;

    public CoursesController(ScheduleContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Courses
            .AsNoTracking()
            .Include(c => c.Sessions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.CourseCrn.Contains(search) ||
                c.CourseName.Contains(search) ||
                c.CourseSection.Contains(search) ||
                c.CourseProfessor.Contains(search) ||
                c.CourseMeetingDays.Contains(search) ||
                c.CourseMeetingTime.Contains(search));
        }

        ViewData["CurrentSearch"] = search;
        return View(await query.OrderBy(c => c.CourseName).ThenBy(c => c.CourseSection).ToListAsync());
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
    public async Task<IActionResult> Create([Bind("CourseID,CourseCrn,CourseName,CourseSection,CourseMeetingDays,CourseMeetingTime,CourseProfessor,CourseLeader,OfficeHoursDay,OfficeHoursTime,OfficeHoursLocation")] Course course)
    {
        if (!ModelState.IsValid)
        {
            PopulateProfessorOptions();
            return View(course);
        }

        _context.Add(course);
        await UpsertCatalogEntryAsync(course.CourseCrn, course.CourseName, course.CourseSection);
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
    public async Task<IActionResult> Edit(int id, [Bind("CourseID,CourseCrn,CourseName,CourseSection,CourseMeetingDays,CourseMeetingTime,CourseProfessor,CourseLeader,OfficeHoursDay,OfficeHoursTime,OfficeHoursLocation")] Course course)
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
            await UpsertCatalogEntryAsync(course.CourseCrn, course.CourseName, course.CourseSection);
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
    public async Task<IActionResult> LookupByCrn(string? crn, int? excludeId)
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
        if (id is null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CourseID == id);

        if (course is null)
        {
            return NotFound();
        }

        return View(course);
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

    private async Task UpsertCatalogEntryAsync(string crn, string courseName, string courseSection)
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
                CourseSection = courseSection.Trim()
            });
            return;
        }

        entry.CourseName = courseName.Trim();
        entry.CourseSection = courseSection.Trim();
    }
}
