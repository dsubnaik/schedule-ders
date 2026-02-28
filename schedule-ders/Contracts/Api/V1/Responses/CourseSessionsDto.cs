namespace schedule_ders.Contracts.Api.V1.Responses;

public class CourseSessionsDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseSection { get; set; } = string.Empty;
    public IReadOnlyList<SessionCardDto> Sessions { get; set; } = [];
}
