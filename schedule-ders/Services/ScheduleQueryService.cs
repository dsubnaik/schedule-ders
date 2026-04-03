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
        var dayValue = day?.Trim() ?? string.Empty;
        var professorValue = professor?.Trim() ?? string.Empty;
        var normalizedSearch = searchValue.ToLower();
        var normalizedTime = timeValue.ToLower();
        var normalizedProfessor = professorValue.ToLower();
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
                (s.Course != null && (s.Course.CourseName.ToLower().Contains(normalizedSearch) || s.Course.CourseTitle.ToLower().Contains(normalizedSearch))) ||
                (s.Course != null && s.Course.CourseSection.ToLower().Contains(normalizedSearch)) ||
                (s.Course != null && s.Course.CourseLeader.ToLower().Contains(normalizedSearch)) ||
                s.Day.ToLower().Contains(normalizedSearch) ||
                s.Location.ToLower().Contains(normalizedSearch) ||
                s.Time.Replace(":", "").Replace(".", "").Replace(" ", "").Replace("-", "").Contains(compactSearch));
        }

        if (!string.IsNullOrWhiteSpace(timeValue) && !hasSearchTime)
        {
            query = query.Where(s =>
                s.Time.ToLower().Contains(normalizedTime) ||
                s.Time.Replace(":", "").Replace(".", "").Replace(" ", "").Replace("-", "").Contains(compactTime));
        }

        if (!string.IsNullOrWhiteSpace(dayValue))
        {
            var normalizedDay = NormalizeDayQuery(dayValue);
            query = query.Where(s => s.Day.ToLower() == normalizedDay);
        }

        if (!string.IsNullOrWhiteSpace(professorValue))
        {
            query = query.Where(s => s.Course != null && s.Course.CourseProfessor.ToLower().Contains(normalizedProfessor));
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

    private static string NormalizeDayQuery(string day)
    {
        var normalized = day.Trim().ToLowerInvariant().Replace(" ", string.Empty);
        return normalized switch
        {
            "m" or "mon" or "monday" => "monday",
            "t" or "tu" or "tue" or "tues" or "tuesday" => "tuesday",
            "w" or "wed" or "wednesday" => "wednesday",
            "r" or "th" or "thu" or "thur" or "thurs" or "thursday" => "thursday",
            "f" or "fri" or "friday" => "friday",
            "sat" or "saturday" => "saturday",
            "sun" or "sunday" => "sunday",
            _ => day.Trim().ToLowerInvariant()
        };
    }
}
