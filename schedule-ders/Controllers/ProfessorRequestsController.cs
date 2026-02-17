using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.ViewModels;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Professor")]
public class ProfessorRequestsController : Controller
{
    private readonly ScheduleContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ProfessorRequestsController(ScheduleContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var requests = await _context.SIRequests
            .AsNoTracking()
            .Where(r => r.CreatedByUserId == userId)
            .Include(r => r.Course)
            .OrderByDescending(r => r.SubmittedAtUtc)
            .ToListAsync();

        return View(requests);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCourseOptionsAsync();
        return View(new ProfessorRequestCreateViewModel
        {
            ProfessorEmail = User.Identity?.Name ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProfessorRequestCreateViewModel input)
    {
        input.RequestedCourseName = input.RequestedCourseName.Trim();
        input.RequestedCourseSection = input.RequestedCourseSection.Trim();
        input.RequestedCourseProfessor = input.RequestedCourseProfessor.Trim();

        var hasManualCourse = !string.IsNullOrWhiteSpace(input.RequestedCourseName) ||
                              !string.IsNullOrWhiteSpace(input.RequestedCourseSection) ||
                              !string.IsNullOrWhiteSpace(input.RequestedCourseProfessor);

        if (!input.CourseID.HasValue && !hasManualCourse)
        {
            ModelState.AddModelError(nameof(input.CourseID), "Select a course or enter course details manually.");
        }

        if (!input.CourseID.HasValue && hasManualCourse)
        {
            if (string.IsNullOrWhiteSpace(input.RequestedCourseName))
            {
                ModelState.AddModelError(nameof(input.RequestedCourseName), "Course name is required when entering manually.");
            }

            if (string.IsNullOrWhiteSpace(input.RequestedCourseSection))
            {
                ModelState.AddModelError(nameof(input.RequestedCourseSection), "Course section is required when entering manually.");
            }

            if (string.IsNullOrWhiteSpace(input.RequestedCourseProfessor))
            {
                ModelState.AddModelError(nameof(input.RequestedCourseProfessor), "Course professor is required when entering manually.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateCourseOptionsAsync(input.CourseID);
            return View(input);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        Course? selectedCourse = null;
        if (input.CourseID.HasValue)
        {
            selectedCourse = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseID == input.CourseID.Value);

            if (selectedCourse is null)
            {
                ModelState.AddModelError(nameof(input.CourseID), "Selected course was not found.");
                await PopulateCourseOptionsAsync(input.CourseID);
                return View(input);
            }
        }

        var request = new SIRequest
        {
            CourseID = input.CourseID,
            RequestedCourseName = selectedCourse?.CourseName ?? input.RequestedCourseName,
            RequestedCourseSection = selectedCourse?.CourseSection ?? input.RequestedCourseSection,
            RequestedCourseProfessor = selectedCourse?.CourseProfessor ?? input.RequestedCourseProfessor,
            ProfessorName = input.ProfessorName,
            ProfessorEmail = input.ProfessorEmail,
            RequestNotes = input.RequestNotes,
            CreatedByUserId = userId,
            Status = SIRequestStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow
        };

        _context.SIRequests.Add(request);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Track), new { id = request.SIRequestID });
    }

    public async Task<IActionResult> Details(int id)
    {
        var request = await GetOwnedRequestAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }

    public async Task<IActionResult> Track(int id)
    {
        var request = await GetOwnedRequestAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }

    private async Task<SIRequest?> GetOwnedRequestAsync(int requestId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _context.SIRequests
            .AsNoTracking()
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.SIRequestID == requestId && r.CreatedByUserId == userId);
    }

    private async Task PopulateCourseOptionsAsync(int? selectedCourseId = null)
    {
        var courses = await _context.Courses
            .AsNoTracking()
            .OrderBy(c => c.CourseName)
            .Select(c => new
            {
                c.CourseID,
                Label = $"{c.CourseName} ({c.CourseSection})"
            })
            .ToListAsync();

        ViewBag.CourseID = new SelectList(courses, "CourseID", "Label", selectedCourseId);
    }
}
