using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Models;
using schedule_ders.Services.Interfaces;
using schedule_ders.ViewModels;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Admin")]
public class AdminRequestsController : Controller
{
    private readonly ScheduleContext _context;
    private readonly IAdminRequestService _adminRequestService;

    public AdminRequestsController(ScheduleContext context, IAdminRequestService adminRequestService)
    {
        _context = context;
        _adminRequestService = adminRequestService;
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

        var updated = await _adminRequestService.UpdateStatusAsync(input.SIRequestID, new UpdateRequestStatusDto
        {
            Status = input.Status,
            AdminNotes = input.AdminNotes
        });

        if (updated is null)
        {
            return NotFound();
        }

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

}
