using Microsoft.EntityFrameworkCore;
using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Contracts.Api.V1.Responses;
using schedule_ders.Models;
using schedule_ders.Services.Interfaces;
using schedule_ders.Utilities;

namespace schedule_ders.Services;

public class ProfessorRequestService : IProfessorRequestService
{
    private readonly ScheduleContext _context;

    public ProfessorRequestService(ScheduleContext context)
    {
        _context = context;
    }

    public async Task<SiRequestSummaryDto> CreateRequestAsync(CreateSiRequestDto input, string createdByUserId)
    {
        Course? selectedCourse = null;

        if (input.CourseId.HasValue)
        {
            selectedCourse = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseID == input.CourseId.Value);

            if (selectedCourse is null)
            {
                throw new KeyNotFoundException("Selected course was not found.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input.RequestedCourseName) ||
                string.IsNullOrWhiteSpace(input.RequestedCourseTitle) ||
                string.IsNullOrWhiteSpace(input.RequestedCourseSection))
            {
                throw new ArgumentException("Course, course name, and section are required when CourseId is not provided.");
            }
        }

        var normalizedPotentialLeaderCandidates = LeaderCandidateCodec.Normalize(input.PotentialSiLeaderName);

        var request = new SIRequest
        {
            CourseID = input.CourseId,
            RequestedCourseName = selectedCourse?.CourseName ?? input.RequestedCourseName!.Trim(),
            RequestedCourseTitle = selectedCourse?.CourseTitle ?? input.RequestedCourseTitle!.Trim(),
            RequestedCourseSection = selectedCourse?.CourseSection ?? input.RequestedCourseSection!.Trim(),
            RequestedCourseProfessor = selectedCourse?.CourseProfessor ?? (input.ProfessorName ?? string.Empty).Trim(),
            ProfessorName = (input.ProfessorName ?? string.Empty).Trim(),
            ProfessorEmail = (input.ProfessorEmail ?? string.Empty).Trim(),
            RequestNotes = input.RequestNotes.Trim(),
            PotentialSiLeaderName = normalizedPotentialLeaderCandidates,
            CreatedByUserId = createdByUserId,
            Status = SIRequestStatus.Pending,
            PotentialSiLeaderStatus = string.IsNullOrWhiteSpace(normalizedPotentialLeaderCandidates)
                ? SILeaderReviewStatus.NotSubmitted
                : SILeaderReviewStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow
        };

        _context.SIRequests.Add(request);
        await _context.SaveChangesAsync();

        var candidateEntries = LeaderCandidateCodec.Parse(normalizedPotentialLeaderCandidates);
        if (candidateEntries.Count > 0)
        {
            _context.SIRequestLeaderCandidates.AddRange(candidateEntries.Select(entry => new SIRequestLeaderCandidate
            {
                SIRequestID = request.SIRequestID,
                CandidateName = entry.CandidateName,
                CandidateANumber = entry.CandidateANumber,
                Status = SILeaderCandidateStatus.Requested
            }));
            await _context.SaveChangesAsync();
        }

        return new SiRequestSummaryDto
        {
            RequestId = request.SIRequestID,
            CourseDisplay = BuildCourseDisplay(request),
            Status = request.Status.ToString(),
            SubmittedAtUtc = request.SubmittedAtUtc
        };
    }

    public async Task<ProfessorRequestStatusLookupResult> GetOwnedStatusAsync(int requestId, string userId)
    {
        var ownedRequest = await _context.SIRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SIRequestID == requestId && r.CreatedByUserId == userId);

        if (ownedRequest is null)
        {
            var exists = await _context.SIRequests
                .AsNoTracking()
                .AnyAsync(r => r.SIRequestID == requestId);

            return new ProfessorRequestStatusLookupResult
            {
                Exists = exists,
                IsOwner = false
            };
        }

        return new ProfessorRequestStatusLookupResult
        {
            Exists = true,
            IsOwner = true,
            Status = new RequestStatusDto
            {
                RequestId = ownedRequest.SIRequestID,
                Status = ownedRequest.Status.ToString(),
                PotentialSiLeaderStatus = ownedRequest.PotentialSiLeaderStatus.ToString(),
                PotentialSiLeaderName = string.IsNullOrWhiteSpace(ownedRequest.PotentialSiLeaderName)
                    ? null
                    : ownedRequest.PotentialSiLeaderName,
                LeaderCandidates = await _context.SIRequestLeaderCandidates
                    .AsNoTracking()
                    .Where(c => c.SIRequestID == ownedRequest.SIRequestID)
                    .OrderBy(c => c.CandidateName)
                    .ThenBy(c => c.CandidateANumber)
                    .Select(c => new LeaderCandidateStatusDto
                    {
                        CandidateId = c.SIRequestLeaderCandidateID,
                        CandidateName = c.CandidateName,
                        CandidateANumber = c.CandidateANumber,
                        Status = c.Status.ToString(),
                        ProgressPercent = GetCandidateProgressPercent(c.Status)
                    })
                    .ToListAsync(),
                SubmittedAtUtc = ownedRequest.SubmittedAtUtc,
                LastUpdatedAtUtc = ownedRequest.LastUpdatedAtUtc,
                AdminNotes = string.IsNullOrWhiteSpace(ownedRequest.AdminNotes) ? null : ownedRequest.AdminNotes,
                ProgressPercent = GetProgressPercent(ownedRequest.Status),
                PotentialSiLeaderProgressPercent = GetPotentialLeaderProgressPercent(ownedRequest.PotentialSiLeaderStatus)
            }
        };
    }

    private static string BuildCourseDisplay(SIRequest request)
    {
        var name = request.RequestedCourseName.Trim();
        var title = request.RequestedCourseTitle.Trim();
        var section = request.RequestedCourseSection.Trim();
        var professor = request.RequestedCourseProfessor.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Manual Course Entry";
        }

        var courseDisplay = string.IsNullOrWhiteSpace(section) ? name : $"{name} ({section})";
        if (!string.IsNullOrWhiteSpace(title))
        {
            courseDisplay = $"{courseDisplay} - {title}";
        }

        return string.IsNullOrWhiteSpace(professor) ? courseDisplay : $"{courseDisplay} - {professor}";
    }

    private static int GetProgressPercent(SIRequestStatus status) =>
        status switch
        {
            SIRequestStatus.Pending => 20,
            SIRequestStatus.UnderReview => 50,
            SIRequestStatus.Approved => 80,
            SIRequestStatus.SiLeaderFound => 100,
            SIRequestStatus.Denied => 100,
            _ => 0
        };

    private static int GetPotentialLeaderProgressPercent(SILeaderReviewStatus status) =>
        status switch
        {
            SILeaderReviewStatus.NotSubmitted => 0,
            SILeaderReviewStatus.Pending => 25,
            SILeaderReviewStatus.UnderReview => 60,
            SILeaderReviewStatus.Approved => 100,
            SILeaderReviewStatus.Denied => 100,
            _ => 0
        };

    private static int GetCandidateProgressPercent(SILeaderCandidateStatus status) =>
        status switch
        {
            SILeaderCandidateStatus.Requested => 25,
            SILeaderCandidateStatus.Vetted => 40,
            SILeaderCandidateStatus.YetToInterview => 55,
            SILeaderCandidateStatus.Interviewed => 75,
            SILeaderCandidateStatus.Hired => 100,
            SILeaderCandidateStatus.NotMovingForward => 100,
            _ => 0
        };
}
