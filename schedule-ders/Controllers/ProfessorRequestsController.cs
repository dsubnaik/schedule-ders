using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Models;
using schedule_ders.Services.Interfaces;
using schedule_ders.ViewModels;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Professor")]
public class ProfessorRequestsController : Controller
{
    private readonly ScheduleContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IProfessorRequestService _professorRequestService;

    public ProfessorRequestsController(
        ScheduleContext context,
        UserManager<IdentityUser> userManager,
        IProfessorRequestService professorRequestService)
    {
        _context = context;
        _userManager = userManager;
        _professorRequestService = professorRequestService;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var query = _context.SIRequests
            .AsNoTracking()
            .Where(r => r.CreatedByUserId == userId)
            .Include(r => r.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SIRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        var requests = await query
            .OrderByDescending(r => r.SubmittedAtUtc)
            .ToListAsync();

        var notificationCookieKey = "sd-prof-last-seen-status";
        var lastSeenRaw = Request.Cookies[notificationCookieKey];
        if (long.TryParse(lastSeenRaw, out var lastSeenUnix))
        {
            var lastSeenUtc = DateTimeOffset.FromUnixTimeSeconds(lastSeenUnix).UtcDateTime;
            var changedCount = await _context.SIRequests
                .AsNoTracking()
                .Where(r => r.CreatedByUserId == userId
                            && r.LastUpdatedAtUtc.HasValue
                            && r.LastUpdatedAtUtc.Value > lastSeenUtc
                            && r.Status != SIRequestStatus.Pending)
                .CountAsync();

            if (changedCount > 0)
            {
                ViewData["ProfessorNotification"] = changedCount == 1
                    ? "1 request status was updated."
                    : $"{changedCount} request statuses were updated.";
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

        ViewData["CurrentStatus"] = status ?? string.Empty;

        return View(requests);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCourseOptionsAsync();
        return View(new ProfessorRequestCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProfessorRequestCreateViewModel input)
    {
        NormalizeAndValidateInput(input);

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

        await TryAssignExistingCourseAsync(input);

        try
        {
            var (professorName, professorEmail) = ResolveProfessorIdentity();

            var created = await _professorRequestService.CreateRequestAsync(new CreateSiRequestDto
            {
                CourseId = input.CourseID,
                RequestedCourseName = input.RequestedCourseName,
                RequestedCourseTitle = input.RequestedCourseTitle,
                RequestedCourseSection = input.RequestedCourseSection,
                RequestedCourseProfessor = professorName,
                ProfessorName = professorName,
                ProfessorEmail = professorEmail,
                RequestNotes = input.RequestNotes,
                PotentialSiLeaderName = input.PotentialSiLeaderName
            }, userId);

            return RedirectToAction(nameof(Track), new { id = created.RequestId });
        }
        catch (KeyNotFoundException)
        {
            ModelState.AddModelError(nameof(input.CourseID), "Selected course was not found.");
            await PopulateCourseOptionsAsync(input.CourseID);
            return View(input);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCourseOptionsAsync(input.CourseID);
            return View(input);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        return RedirectToAction(nameof(Track), new { id });
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

    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var request = await _context.SIRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SIRequestID == id && r.CreatedByUserId == userId);

        if (request is null)
        {
            return NotFound();
        }

        await PopulateCourseOptionsAsync(request.CourseID);

        return View(new ProfessorRequestCreateViewModel
        {
            CourseID = request.CourseID,
            RequestedCourseName = request.RequestedCourseName,
            RequestedCourseTitle = request.RequestedCourseTitle,
            RequestedCourseSection = request.RequestedCourseSection,
            RequestedCourseProfessor = request.RequestedCourseProfessor,
            RequestNotes = request.RequestNotes,
            PotentialSiLeaderName = request.PotentialSiLeaderName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProfessorRequestCreateViewModel input)
    {
        NormalizeAndValidateInput(input);

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

        var request = await _context.SIRequests
            .FirstOrDefaultAsync(r => r.SIRequestID == id && r.CreatedByUserId == userId);

        if (request is null)
        {
            return NotFound();
        }

        await TryAssignExistingCourseAsync(input);

        if (input.CourseID.HasValue)
        {
            var exists = await _context.Courses.AnyAsync(c => c.CourseID == input.CourseID.Value);
            if (!exists)
            {
                ModelState.AddModelError(nameof(input.CourseID), "Selected course was not found.");
                await PopulateCourseOptionsAsync(input.CourseID);
                return View(input);
            }
        }

        request.CourseID = input.CourseID;
        request.RequestedCourseName = input.RequestedCourseName;
        request.RequestedCourseTitle = input.RequestedCourseTitle;
        request.RequestedCourseSection = input.RequestedCourseSection;
        var (professorName, professorEmail) = ResolveProfessorIdentity();
        request.RequestedCourseProfessor = professorName;
        request.ProfessorName = professorName;
        request.ProfessorEmail = professorEmail;
        request.RequestNotes = input.RequestNotes;
        ApplyPotentialLeaderUpdate(request, input.PotentialSiLeaderName);
        await SyncLeaderCandidatesAsync(request.SIRequestID, input.PotentialSiLeaderName);
        request.LastUpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = request.SIRequestID });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var request = await _context.SIRequests
            .FirstOrDefaultAsync(r => r.SIRequestID == id && r.CreatedByUserId == userId);

        if (request is null)
        {
            return NotFound();
        }

        _context.SIRequests.Remove(request);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
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
            .Include(r => r.LeaderCandidates)
            .FirstOrDefaultAsync(r => r.SIRequestID == requestId && r.CreatedByUserId == userId);
    }

    private async Task PopulateCourseOptionsAsync(int? selectedCourseId = null)
    {
        var courses = await _context.Courses
            .AsNoTracking()
            .OrderBy(c => c.CourseName)
            .ThenBy(c => c.CourseSection)
            .Select(c => new
            {
                c.CourseID,
                c.CourseName,
                c.CourseTitle,
                Label = $"{c.CourseName} - {c.CourseTitle} ({c.CourseSection})"
            })
            .ToListAsync();

        ViewBag.CourseID = new SelectList(courses, "CourseID", "Label", selectedCourseId);
        ViewBag.CourseCodeOptions = courses
            .Select(c => c.CourseName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        ViewBag.CourseTitleOptions = courses
            .Select(c => c.CourseTitle)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        ViewBag.SiLeaderOptions = await _context.Courses
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseLeader))
            .Select(c => c.CourseLeader)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    private void NormalizeAndValidateInput(ProfessorRequestCreateViewModel input)
    {
        input.RequestedCourseName = input.RequestedCourseName.Trim();
        input.RequestedCourseTitle = input.RequestedCourseTitle.Trim();
        input.RequestedCourseSection = input.RequestedCourseSection.Trim();
        input.RequestedCourseProfessor = input.RequestedCourseProfessor.Trim();
        input.PotentialSiLeaderName = NormalizePotentialLeaderCandidates(input.PotentialSiLeaderName);

        var hasManualCourse = !string.IsNullOrWhiteSpace(input.RequestedCourseName) ||
                              !string.IsNullOrWhiteSpace(input.RequestedCourseTitle) ||
                              !string.IsNullOrWhiteSpace(input.RequestedCourseSection);

        if (!input.CourseID.HasValue && !hasManualCourse)
        {
            ModelState.AddModelError(nameof(input.CourseID), "Select a course or enter course details manually.");
        }

        if (!input.CourseID.HasValue && hasManualCourse)
        {
            if (string.IsNullOrWhiteSpace(input.RequestedCourseName))
            {
                ModelState.AddModelError(nameof(input.RequestedCourseName), "Course code is required when entering manually.");
            }

            if (string.IsNullOrWhiteSpace(input.RequestedCourseTitle))
            {
                ModelState.AddModelError(nameof(input.RequestedCourseTitle), "Course name is required when entering manually.");
            }

            if (string.IsNullOrWhiteSpace(input.RequestedCourseSection))
            {
                ModelState.AddModelError(nameof(input.RequestedCourseSection), "Course section is required when entering manually.");
            }
        }
    }

    private async Task TryAssignExistingCourseAsync(ProfessorRequestCreateViewModel input)
    {
        if (input.CourseID.HasValue)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(input.RequestedCourseName) || string.IsNullOrWhiteSpace(input.RequestedCourseSection))
        {
            return;
        }

        var name = input.RequestedCourseName.Trim();
        var section = input.RequestedCourseSection.Trim();

        var existingCourseId = await _context.Courses
            .AsNoTracking()
            .Where(c => c.CourseName == name
                        && c.CourseSection == section
                        && (string.IsNullOrWhiteSpace(input.RequestedCourseTitle) || c.CourseTitle == input.RequestedCourseTitle))
            .Select(c => (int?)c.CourseID)
            .FirstOrDefaultAsync();

        if (existingCourseId.HasValue)
        {
            input.CourseID = existingCourseId.Value;
        }
    }

    private (string Name, string Email) ResolveProfessorIdentity()
    {
        var email = (User.Identity?.Name ?? string.Empty).Trim();
        var localPart = email.Contains('@') ? email.Split('@')[0] : email;
        var safeLocalPart = string.IsNullOrWhiteSpace(localPart) ? "Professor" : localPart;
        var displayName = safeLocalPart
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Professor";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            email = "professor@schedule-ders.local";
        }

        return (displayName, email);
    }

    private static void ApplyPotentialLeaderUpdate(SIRequest request, string? potentialLeaderName)
    {
        var normalized = NormalizePotentialLeaderCandidates(potentialLeaderName);
        var current = request.PotentialSiLeaderName?.Trim() ?? string.Empty;
        var changed = !string.Equals(normalized, current, StringComparison.Ordinal);

        request.PotentialSiLeaderName = normalized;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            request.PotentialSiLeaderStatus = SILeaderReviewStatus.NotSubmitted;
            return;
        }

        if (changed || request.PotentialSiLeaderStatus == SILeaderReviewStatus.NotSubmitted)
        {
            request.PotentialSiLeaderStatus = SILeaderReviewStatus.Pending;
        }
    }

    private async Task SyncLeaderCandidatesAsync(int requestId, string? potentialLeaderName)
    {
        var normalizedCandidates = NormalizePotentialLeaderCandidates(potentialLeaderName)
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var existing = await _context.SIRequestLeaderCandidates
            .Where(c => c.SIRequestID == requestId)
            .ToListAsync();

        var toRemove = existing
            .Where(c => !normalizedCandidates.Any(name => string.Equals(name, c.CandidateName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (toRemove.Count > 0)
        {
            _context.SIRequestLeaderCandidates.RemoveRange(toRemove);
        }

        foreach (var candidateName in normalizedCandidates)
        {
            var match = existing.FirstOrDefault(c => string.Equals(c.CandidateName, candidateName, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                _context.SIRequestLeaderCandidates.Add(new SIRequestLeaderCandidate
                {
                    SIRequestID = requestId,
                    CandidateName = candidateName,
                    Status = SILeaderCandidateStatus.Requested
                });
            }
        }
    }

    private static string NormalizePotentialLeaderCandidates(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        var candidates = rawValue
            .Replace("\r", "\n")
            .Split(new[] { '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return candidates.Count == 0 ? string.Empty : string.Join('\n', candidates);
    }
}
