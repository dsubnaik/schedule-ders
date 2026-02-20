using schedule_ders.Contracts.Api.V1.Responses;

namespace schedule_ders.Services.Interfaces;

public interface IScheduleQueryService
{
    Task<PagedResultDto<SessionCardDto>> SearchSessionsAsync(
        string? search,
        string? day,
        string? professor,
        int? courseId,
        int page,
        int pageSize);

    Task<CourseSessionsDto?> GetCourseSessionsAsync(int courseId);
}
