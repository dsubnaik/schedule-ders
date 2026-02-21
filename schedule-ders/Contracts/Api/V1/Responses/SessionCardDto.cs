namespace schedule_ders.Contracts.Api.V1.Responses;

public class SessionCardDto
{
    public int SessionId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseSection { get; set; } = string.Empty;
    public string ProfessorName { get; set; } = string.Empty;
    public string SiLeaderName { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
