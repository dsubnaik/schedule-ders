using Microsoft.EntityFrameworkCore;
using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Contracts.Api.V1.Responses;
using schedule_ders.Models;
using schedule_ders.Services.Interfaces;

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
            CreatedByUserId = createdByUserId,
            Status = SIRequestStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow
        };

        _context.SIRequests.Add(request);
        await _context.SaveChangesAsync();

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
                SubmittedAtUtc = ownedRequest.SubmittedAtUtc,
                LastUpdatedAtUtc = ownedRequest.LastUpdatedAtUtc,
                AdminNotes = string.IsNullOrWhiteSpace(ownedRequest.AdminNotes) ? null : ownedRequest.AdminNotes,
                ProgressPercent = GetProgressPercent(ownedRequest.Status)
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
            SIRequestStatus.Pending => 25,
            SIRequestStatus.UnderReview => 60,
            SIRequestStatus.Approved => 100,
            SIRequestStatus.Denied => 100,
            _ => 0
        };
}
