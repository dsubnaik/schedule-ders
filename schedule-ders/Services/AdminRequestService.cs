using Microsoft.EntityFrameworkCore;
using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Contracts.Api.V1.Responses;
using schedule_ders.Models;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Services;

public class AdminRequestService : IAdminRequestService
{
    private readonly ScheduleContext _context;
    private readonly IRequestNotificationEmailService _notificationEmailService;

    public AdminRequestService(
        ScheduleContext context,
        IRequestNotificationEmailService notificationEmailService)
    {
        _context = context;
        _notificationEmailService = notificationEmailService;
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
                (r.Course != null && (r.Course.CourseName.Contains(course) || r.Course.CourseTitle.Contains(course))) ||
                r.RequestedCourseName.Contains(course) ||
                r.RequestedCourseTitle.Contains(course) ||
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
                PotentialSiLeaderStatus = r.PotentialSiLeaderStatus.ToString(),
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

    public async Task<SiRequestSummaryDto?> UpdateStatusAsync(int requestId, UpdateRequestStatusDto input, int? semesterId = null)
    {
        var request = await _context.SIRequests
            .Include(r => r.Course)
            .Include(r => r.LeaderCandidates)
            .FirstOrDefaultAsync(r => r.SIRequestID == requestId);

        if (request is null)
        {
            return null;
        }

        request.Status = input.Status;
        if (input.PotentialSiLeaderStatus.HasValue)
        {
            request.PotentialSiLeaderStatus = input.PotentialSiLeaderStatus.Value;
        }
        request.AdminNotes = input.AdminNotes?.Trim() ?? string.Empty;
        request.LastUpdatedAtUtc = DateTime.UtcNow;

        if (request.Status is SIRequestStatus.Approved or SIRequestStatus.SiLeaderFound)
        {
            await EnsureCourseLinkedForApprovedRequestAsync(request, semesterId);
        }

        if (request.Status == SIRequestStatus.SiLeaderFound)
        {
            await EnsureSiLeadersForHiredCandidatesAsync(request);
        }

        await _context.SaveChangesAsync();
        await _notificationEmailService.NotifyRequestStatusUpdatedAsync(request);

        return new SiRequestSummaryDto
        {
            RequestId = request.SIRequestID,
            CourseDisplay = BuildCourseDisplay(request),
            Status = request.Status.ToString(),
            SubmittedAtUtc = request.SubmittedAtUtc
        };
    }

    public async Task<RemoveAdminRequestResult> RemoveRequestAsync(int requestId)
    {
        var request = await _context.SIRequests
            .FirstOrDefaultAsync(r => r.SIRequestID == requestId);

        if (request is null)
        {
            return RemoveAdminRequestResult.NotFound;
        }

        if (request.Status != SIRequestStatus.Approved
            && request.Status != SIRequestStatus.SiLeaderFound
            && request.Status != SIRequestStatus.Denied)
        {
            return RemoveAdminRequestResult.NotAllowed;
        }

        _context.SIRequests.Remove(request);
        await _context.SaveChangesAsync();
        return RemoveAdminRequestResult.Removed;
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

    private async Task EnsureCourseLinkedForApprovedRequestAsync(SIRequest request, int? semesterId)
    {
        var validSemesterId = await ResolveValidSemesterIdAsync(semesterId);

        if (request.CourseID.HasValue)
        {
            var linkedCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == request.CourseID.Value);
            if (linkedCourse is not null)
            {
                if (validSemesterId.HasValue && !linkedCourse.SemesterId.HasValue)
                {
                    linkedCourse.SemesterId = validSemesterId.Value;
                }

                request.Course = linkedCourse;
                return;
            }
        }

        var requestedName = request.RequestedCourseName.Trim();
        var requestedTitle = request.RequestedCourseTitle.Trim();
        var requestedSection = request.RequestedCourseSection.Trim();
        var requestedProfessor = request.RequestedCourseProfessor.Trim();

        if (string.IsNullOrWhiteSpace(requestedName) || string.IsNullOrWhiteSpace(requestedTitle) || string.IsNullOrWhiteSpace(requestedSection))
        {
            return;
        }

        var existingCourse = await _context.Courses
            .FirstOrDefaultAsync(c => c.CourseName == requestedName
                                      && c.CourseSection == requestedSection
                                      && c.CourseTitle == requestedTitle);

        if (existingCourse is not null)
        {
            if (validSemesterId.HasValue && !existingCourse.SemesterId.HasValue)
            {
                existingCourse.SemesterId = validSemesterId.Value;
            }

            request.CourseID = existingCourse.CourseID;
            request.Course = existingCourse;
            return;
        }

        var createdCourse = new Course
        {
            CourseCrn = $"REQ-{request.SIRequestID}",
            CourseName = requestedName,
            CourseTitle = requestedTitle,
            CourseSection = requestedSection,
            CourseMeetingDays = "T",
            CourseMeetingTime = "12:00pm-1:00pm",
            CourseProfessor = string.IsNullOrWhiteSpace(requestedProfessor) ? "TBD" : requestedProfessor,
            CourseLeader = "TBD",
            OfficeHoursDay = string.Empty,
            OfficeHoursTime = string.Empty,
            OfficeHoursLocation = string.Empty,
            SemesterId = validSemesterId
        };

        _context.Courses.Add(createdCourse);
        await _context.SaveChangesAsync();
        request.CourseID = createdCourse.CourseID;
        request.Course = createdCourse;
    }

    private async Task<int?> ResolveValidSemesterIdAsync(int? semesterId)
    {
        if (!semesterId.HasValue)
        {
            return null;
        }

        return await _context.Semesters.AnyAsync(s => s.SemesterId == semesterId.Value)
            ? semesterId.Value
            : null;
    }

    private async Task EnsureSiLeadersForHiredCandidatesAsync(SIRequest request)
    {
        var hiredCandidates = request.LeaderCandidates
            .Where(c => c.Status == SILeaderCandidateStatus.Hired)
            .OrderBy(c => c.CandidateName)
            .ThenBy(c => c.CandidateANumber)
            .ToList();

        if (hiredCandidates.Count == 0)
        {
            return;
        }

        var course = request.Course;
        if (course is null && request.CourseID.HasValue)
        {
            course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == request.CourseID.Value);
        }

        if (course is null)
        {
            return;
        }

        var primaryLeaderName = hiredCandidates[0].CandidateName.Trim();
        if (!string.IsNullOrWhiteSpace(primaryLeaderName))
        {
            course.CourseLeader = primaryLeaderName;
        }

        foreach (var candidate in hiredCandidates)
        {
            var leaderName = candidate.CandidateName.Trim();
            if (string.IsNullOrWhiteSpace(leaderName))
            {
                continue;
            }

            var aNumber = candidate.CandidateANumber.Trim();
            SILeader? leader = null;
            if (!string.IsNullOrWhiteSpace(aNumber))
            {
                leader = await _context.SILeaders
                    .FirstOrDefaultAsync(l => l.ANumber == aNumber);
            }

            leader ??= await _context.SILeaders
                .FirstOrDefaultAsync(l => l.LeaderName.ToLower() == leaderName.ToLower());

            if (leader is null)
            {
                leader = new SILeader
                {
                    ANumber = string.IsNullOrWhiteSpace(aNumber) ? GeneratePlaceholderANumber() : aNumber,
                    LeaderName = leaderName,
                    StoredCourseAssignments = BuildAssignment(course.CourseName, course.CourseSection)
                };
                _context.SILeaders.Add(leader);
                continue;
            }

            leader.LeaderName = leaderName;
            leader.StoredCourseAssignments = MergeAssignments(
                leader.StoredCourseAssignments,
                course.CourseName,
                course.CourseSection);
        }
    }

    private static string BuildAssignment(string courseName, string courseSection)
    {
        return $"{courseName.Trim()}|{courseSection.Trim()}";
    }

    private static string MergeAssignments(string? existingAssignments, string courseName, string courseSection)
    {
        var combined = ParseAssignments(existingAssignments)
            .Append((courseName.Trim(), courseSection.Trim()))
            .Distinct()
            .Select(x => $"{x.Item1}|{x.Item2}")
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        return string.Join(Environment.NewLine, combined);
    }

    private static List<(string CourseName, string CourseSection)> ParseAssignments(string? rawAssignments)
    {
        if (string.IsNullOrWhiteSpace(rawAssignments))
        {
            return [];
        }

        return rawAssignments
            .Replace("\r", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length < 2)
                {
                    return (CourseName: string.Empty, CourseSection: string.Empty);
                }

                return (CourseName: parts[0], CourseSection: parts[1]);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.CourseName) && !string.IsNullOrWhiteSpace(x.CourseSection))
            .Distinct()
            .ToList();
    }

    private static string GeneratePlaceholderANumber()
    {
        return $"TMP{Guid.NewGuid():N}"[..11].ToUpperInvariant();
    }
}
