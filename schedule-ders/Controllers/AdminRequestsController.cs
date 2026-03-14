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
            .Include(r => r.LeaderCandidates)
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
            PotentialSiLeaderName = request.PotentialSiLeaderName,
            LeaderCandidates = request.LeaderCandidates
                .OrderBy(c => c.CandidateName)
                .Select(c => new AdminLeaderCandidateStatusItemViewModel
                {
                    CandidateId = c.SIRequestLeaderCandidateID,
                    CandidateName = c.CandidateName,
                    Status = c.Status
                })
                .ToList(),
            AdminNotes = request.AdminNotes
        };

        ViewBag.Statuses = new SelectList(Enum.GetValues<SIRequestStatus>());
        ViewBag.CandidateStatuses = new SelectList(Enum.GetValues<SILeaderCandidateStatus>());
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AdminRequestStatusUpdateViewModel input)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Statuses = new SelectList(Enum.GetValues<SIRequestStatus>());
            ViewBag.CandidateStatuses = new SelectList(Enum.GetValues<SILeaderCandidateStatus>());
            return View(input);
        }

        var request = await _context.SIRequests
            .Include(r => r.LeaderCandidates)
            .FirstOrDefaultAsync(r => r.SIRequestID == input.SIRequestID);
        if (request is null)
        {
            return NotFound();
        }

        request.Status = input.Status;
        request.AdminNotes = input.AdminNotes?.Trim() ?? string.Empty;
        request.LastUpdatedAtUtc = DateTime.UtcNow;

        var candidateUpdates = input.LeaderCandidates ?? [];
        foreach (var candidate in request.LeaderCandidates)
        {
            var update = candidateUpdates.FirstOrDefault(x => x.CandidateId == candidate.SIRequestLeaderCandidateID);
            if (update is null)
            {
                continue;
            }

            candidate.Status = update.Status;
            candidate.LastUpdatedAtUtc = DateTime.UtcNow;
        }

        request.PotentialSiLeaderName = string.Join('\n', request.LeaderCandidates
            .OrderBy(c => c.CandidateName)
            .Select(c => c.CandidateName));
        request.PotentialSiLeaderStatus = MapAggregateLeaderStatus(request.LeaderCandidates);
        if (request.LeaderCandidates.Any(c => c.Status == SILeaderCandidateStatus.Hired)
            && request.Status != SIRequestStatus.Denied)
        {
            request.Status = SIRequestStatus.SiLeaderFound;
        }

        await _context.SaveChangesAsync();

        if (request.Status == SIRequestStatus.Approved || request.Status == SIRequestStatus.SiLeaderFound)
        {
            await _adminRequestService.UpdateStatusAsync(input.SIRequestID, new UpdateRequestStatusDto
            {
                Status = request.Status,
                AdminNotes = request.AdminNotes
            });
        }

        return RedirectToAction(nameof(Index));
    }

    private static string BuildCourseDisplay(SIRequest request)
    {
        if (request.Course is not null)
        {
            return $"{request.Course.CourseName} ({request.Course.CourseSection}) - {request.Course.CourseTitle}";
        }

        var name = request.RequestedCourseName.Trim();
        var title = request.RequestedCourseTitle.Trim();
        var section = request.RequestedCourseSection.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Manual Course Entry";
        }

        var code = string.IsNullOrWhiteSpace(section) ? name : $"{name} ({section})";
        return string.IsNullOrWhiteSpace(title) ? code : $"{code} - {title}";
    }

    private static SILeaderReviewStatus MapAggregateLeaderStatus(IEnumerable<SIRequestLeaderCandidate> candidates)
    {
        var list = candidates.ToList();
        if (list.Count == 0)
        {
            return SILeaderReviewStatus.NotSubmitted;
        }

        if (list.Any(c => c.Status == SILeaderCandidateStatus.Hired))
        {
            return SILeaderReviewStatus.Approved;
        }

        if (list.Any(c => c.Status is SILeaderCandidateStatus.YetToInterview or SILeaderCandidateStatus.Interviewed))
        {
            return SILeaderReviewStatus.UnderReview;
        }

        return SILeaderReviewStatus.Pending;
    }
}
