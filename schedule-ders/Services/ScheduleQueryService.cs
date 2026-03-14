using Microsoft.EntityFrameworkCore;
using schedule_ders.Contracts.Api.V1.Responses;
using schedule_ders.Services.Interfaces;
using schedule_ders.Utilities;

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
        string? time,
        string? day,
        string? professor,
        int? semesterId,
        int? courseId,
        int page,
        int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var searchValue = search?.Trim() ?? string.Empty;
        var timeValue = time?.Trim() ?? string.Empty;
        var compactTime = TimeSearchHelper.ToCompactToken(timeValue);
        var hasSearchTime = TimeSearchHelper.TryParseSearchTime(timeValue, out _);
        var compactSearch = TimeSearchHelper.ToCompactToken(searchValue);

        var query = _context.Sessions
            .AsNoTracking()
            .Include(s => s.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(s =>
                (s.Course != null && (s.Course.CourseName.Contains(searchValue) || s.Course.CourseTitle.Contains(searchValue))) ||
                (s.Course != null && s.Course.CourseSection.Contains(searchValue)) ||
                (s.Course != null && s.Course.CourseLeader.Contains(searchValue)) ||
                s.Day.Contains(searchValue) ||
                s.Location.Contains(searchValue) ||
                s.Time.Replace(":", "").Replace(".", "").Replace(" ", "").Replace("-", "").Contains(compactSearch));
        }

        if (!string.IsNullOrWhiteSpace(timeValue) && !hasSearchTime)
        {
            query = query.Where(s =>
                s.Time.Contains(timeValue) ||
                s.Time.Replace(":", "").Replace(".", "").Replace(" ", "").Replace("-", "").Contains(compactTime));
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

        if (semesterId.HasValue)
        {
            query = query.Where(s => s.Course != null && s.Course.SemesterId == semesterId.Value);
        }

        List<SessionCardDto> items;
        int totalCount;

        if (hasSearchTime)
        {
            var allFiltered = await query
                .OrderBy(s => s.Day)
                .ThenBy(s => s.Time)
                .Select(s => new SessionCardDto
                {
                    SessionId = s.SessionID,
                    CourseId = s.CourseID,
                    CourseName = s.Course != null ? s.Course.CourseName : string.Empty,
                    CourseTitle = s.Course != null ? s.Course.CourseTitle : string.Empty,
                    CourseSection = s.Course != null ? s.Course.CourseSection : string.Empty,
                    ProfessorName = s.Course != null ? s.Course.CourseProfessor : string.Empty,
                    SiLeaderName = s.Course != null ? s.Course.CourseLeader : string.Empty,
                    Day = s.Day,
                    StartTime = GetStartTime(s.Time),
                    EndTime = GetEndTime(s.Time),
                    Location = s.Location
                })
                .ToListAsync();

            var narrowed = allFiltered
                .Where(s =>
                    TimeSearchHelper.MatchesTimeRange($"{s.StartTime}-{s.EndTime}", timeValue) ||
                    TimeSearchHelper.MatchesTimeText($"{s.StartTime}-{s.EndTime}", timeValue))
                .ToList();

            totalCount = narrowed.Count;
            items = narrowed
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        else
        {
            totalCount = await query.CountAsync();
            items = await query
                .OrderBy(s => s.Day)
                .ThenBy(s => s.Time)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SessionCardDto
                {
                    SessionId = s.SessionID,
                    CourseId = s.CourseID,
                    CourseName = s.Course != null ? s.Course.CourseName : string.Empty,
                    CourseTitle = s.Course != null ? s.Course.CourseTitle : string.Empty,
                    CourseSection = s.Course != null ? s.Course.CourseSection : string.Empty,
                    ProfessorName = s.Course != null ? s.Course.CourseProfessor : string.Empty,
                    SiLeaderName = s.Course != null ? s.Course.CourseLeader : string.Empty,
                    Day = s.Day,
                    StartTime = GetStartTime(s.Time),
                    EndTime = GetEndTime(s.Time),
                    Location = s.Location
                })
                .ToListAsync();
        }

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
                CourseTitle = course.CourseTitle,
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
            CourseTitle = course.CourseTitle,
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
