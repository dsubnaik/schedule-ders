using Microsoft.EntityFrameworkCore;
using schedule_ders.Contracts.Api.V1.Responses;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Services;

public class ScheduleQueryService : IScheduleQueryService
{
    private readonly ScheduleContext _context;

    public ScheduleQueryService(ScheduleContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<SessionCardDto>> SearchSessionsAsync(
        string? search,
        string? day,
        string? professor,
        int? courseId,
        int page,
        int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Sessions
            .AsNoTracking()
            .Include(s => s.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                (s.Course != null && s.Course.CourseName.Contains(search)) ||
                (s.Course != null && s.Course.CourseSection.Contains(search)) ||
                (s.Course != null && s.Course.CourseLeader.Contains(search)) ||
                s.Day.Contains(search) ||
                s.Time.Contains(search) ||
                s.Location.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(day))
        {
            query = query.Where(s => s.Day == day);
        }

        if (!string.IsNullOrWhiteSpace(professor))
        {
            query = query.Where(s => s.Course != null && s.Course.CourseProfessor.Contains(professor));
        }

        if (courseId.HasValue)
        {
            query = query.Where(s => s.CourseID == courseId.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Day)
            .ThenBy(s => s.Time)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SessionCardDto
            {
                SessionId = s.SessionID,
                CourseId = s.CourseID,
                CourseName = s.Course != null ? s.Course.CourseName : string.Empty,
                CourseSection = s.Course != null ? s.Course.CourseSection : string.Empty,
                ProfessorName = s.Course != null ? s.Course.CourseProfessor : string.Empty,
                SiLeaderName = s.Course != null ? s.Course.CourseLeader : string.Empty,
                Day = s.Day,
                StartTime = GetStartTime(s.Time),
                EndTime = GetEndTime(s.Time),
                Location = s.Location
            })
            .ToListAsync();

        return new PagedResultDto<SessionCardDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CourseSessionsDto?> GetCourseSessionsAsync(int courseId)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CourseID == courseId);

        if (course is null)
        {
            return null;
        }

        var sessions = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.CourseID == courseId)
            .OrderBy(s => s.Day)
            .ThenBy(s => s.Time)
            .Select(s => new SessionCardDto
            {
                SessionId = s.SessionID,
                CourseId = s.CourseID,
                CourseName = course.CourseName,
                CourseSection = course.CourseSection,
                ProfessorName = course.CourseProfessor,
                SiLeaderName = course.CourseLeader,
                Day = s.Day,
                StartTime = GetStartTime(s.Time),
                EndTime = GetEndTime(s.Time),
                Location = s.Location
            })
            .ToListAsync();

        return new CourseSessionsDto
        {
            CourseId = course.CourseID,
            CourseName = course.CourseName,
            CourseSection = course.CourseSection,
            Sessions = sessions
        };
    }

    private static string GetStartTime(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            return string.Empty;
        }

        var splitIndex = time.IndexOf('-');
        return splitIndex >= 0 ? time[..splitIndex].Trim() : time.Trim();
    }

    private static string GetEndTime(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            return string.Empty;
        }

        var splitIndex = time.IndexOf('-');
        return splitIndex >= 0 && splitIndex + 1 < time.Length
            ? time[(splitIndex + 1)..].Trim()
            : string.Empty;
    }
}
