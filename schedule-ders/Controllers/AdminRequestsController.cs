using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.ViewModels;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Admin")]
public class AdminRequestsController : Controller
{
    private readonly ScheduleContext _context;

    public AdminRequestsController(ScheduleContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var query = _context.SIRequests
            .AsNoTracking()
            .Include(r => r.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SIRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        ViewData["CurrentStatus"] = status ?? string.Empty;
        ViewBag.Statuses = new SelectList(Enum.GetNames<SIRequestStatus>());

        var requests = await query
            .OrderByDescending(r => r.SubmittedAtUtc)
            .ToListAsync();

        return View(requests);
    }

    public async Task<IActionResult> Update(int id)
    {
        var request = await _context.SIRequests
            .AsNoTracking()
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.SIRequestID == id);

        if (request is null)
        {
            return NotFound();
        }

        var vm = new AdminRequestStatusUpdateViewModel
        {
            SIRequestID = request.SIRequestID,
            CourseID = request.CourseID,
            CourseDisplay = BuildCourseDisplay(request),
            ProfessorName = request.ProfessorName,
            ProfessorEmail = request.ProfessorEmail,
            RequestNotes = request.RequestNotes,
            SubmittedAtUtc = request.SubmittedAtUtc,
            Status = request.Status,
            AdminNotes = request.AdminNotes
        };

        ViewBag.Statuses = new SelectList(Enum.GetValues<SIRequestStatus>());
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AdminRequestStatusUpdateViewModel input)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Statuses = new SelectList(Enum.GetValues<SIRequestStatus>());
            return View(input);
        }

        var request = await _context.SIRequests.FirstOrDefaultAsync(r => r.SIRequestID == input.SIRequestID);
        if (request is null)
        {
            return NotFound();
        }

        request.Status = input.Status;
        request.AdminNotes = input.AdminNotes.Trim();
        request.LastUpdatedAtUtc = DateTime.UtcNow;

        if (request.Status == SIRequestStatus.Approved)
        {
            await EnsureCourseLinkedForApprovedRequestAsync(request);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static string BuildCourseDisplay(SIRequest request)
    {
        if (request.Course is not null)
        {
            return $"{request.Course.CourseName} ({request.Course.CourseSection})";
        }

        var name = request.RequestedCourseName.Trim();
        var section = request.RequestedCourseSection.Trim();
        var professor = request.RequestedCourseProfessor.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Manual Course Entry";
        }

        var title = string.IsNullOrWhiteSpace(section) ? name : $"{name} ({section})";
        return string.IsNullOrWhiteSpace(professor) ? title : $"{title} - {professor}";
    }

    private async Task EnsureCourseLinkedForApprovedRequestAsync(SIRequest request)
    {
        if (request.CourseID.HasValue)
        {
            var linkedExists = await _context.Courses.AnyAsync(c => c.CourseID == request.CourseID.Value);
            if (linkedExists)
            {
                return;
            }
        }

        var requestedName = request.RequestedCourseName.Trim();
        var requestedSection = request.RequestedCourseSection.Trim();
        var requestedProfessor = request.RequestedCourseProfessor.Trim();

        if (string.IsNullOrWhiteSpace(requestedName) || string.IsNullOrWhiteSpace(requestedSection))
        {
            return;
        }

        var existingCourse = await _context.Courses
            .FirstOrDefaultAsync(c =>
                c.CourseName == requestedName &&
                c.CourseSection == requestedSection);

        if (existingCourse is not null)
        {
            request.CourseID = existingCourse.CourseID;
            return;
        }

        var createdCourse = new Course
        {
            CourseCrn = $"REQ-{request.SIRequestID}",
            CourseName = requestedName,
            CourseSection = requestedSection,
            CourseMeetingDays = "T",
            CourseMeetingTime = "12:00pm-1:00pm",
            CourseProfessor = string.IsNullOrWhiteSpace(requestedProfessor) ? "TBD" : requestedProfessor,
            CourseLeader = "TBD",
            OfficeHoursDay = "",
            OfficeHoursTime = "",
            OfficeHoursLocation = ""
        };

        _context.Courses.Add(createdCourse);
        await _context.SaveChangesAsync();

        request.CourseID = createdCourse.CourseID;
    }
}
