using Microsoft.EntityFrameworkCore;
using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Contracts.Api.V1.Responses;
using schedule_ders.Models;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Services;

public class AdminRequestService : IAdminRequestService
{
    private readonly ScheduleContext _context;

    public AdminRequestService(ScheduleContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<AdminRequestListItemDto>> GetRequestsAsync(
        string? status,
        string? course,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.SIRequests
            .AsNoTracking()
            .Include(r => r.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SIRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(course))
        {
            query = query.Where(r =>
                (r.Course != null && r.Course.CourseName.Contains(course)) ||
                r.RequestedCourseName.Contains(course) ||
                r.RequestedCourseSection.Contains(course));
        }

        if (from.HasValue)
        {
            query = query.Where(r => r.SubmittedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.SubmittedAtUtc <= to.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.SubmittedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminRequestListItemDto
            {
                RequestId = r.SIRequestID,
                CourseId = r.CourseID,
                CourseDisplay = BuildCourseDisplay(r),
                ProfessorName = r.ProfessorName,
                ProfessorEmail = r.ProfessorEmail,
                Status = r.Status.ToString(),
                SubmittedAtUtc = r.SubmittedAtUtc,
                LastUpdatedAtUtc = r.LastUpdatedAtUtc
            })
            .ToListAsync();

        return new PagedResultDto<AdminRequestListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<SiRequestSummaryDto?> UpdateStatusAsync(int requestId, UpdateRequestStatusDto input)
    {
        var request = await _context.SIRequests
            .FirstOrDefaultAsync(r => r.SIRequestID == requestId);

        if (request is null)
        {
            return null;
        }

        request.Status = input.Status;
        request.AdminNotes = input.AdminNotes?.Trim() ?? string.Empty;
        request.LastUpdatedAtUtc = DateTime.UtcNow;

        if (request.Status == SIRequestStatus.Approved)
        {
            await EnsureCourseLinkedForApprovedRequestAsync(request);
        }

        await _context.SaveChangesAsync();

        return new SiRequestSummaryDto
        {
            RequestId = request.SIRequestID,
            CourseDisplay = BuildCourseDisplay(request),
            Status = request.Status.ToString(),
            SubmittedAtUtc = request.SubmittedAtUtc
        };
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
            .FirstOrDefaultAsync(c => c.CourseName == requestedName && c.CourseSection == requestedSection);

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
            OfficeHoursDay = string.Empty,
            OfficeHoursTime = string.Empty,
            OfficeHoursLocation = string.Empty
        };

        _context.Courses.Add(createdCourse);
        await _context.SaveChangesAsync();
        request.CourseID = createdCourse.CourseID;
    }
}
