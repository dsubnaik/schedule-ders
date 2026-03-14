namespace schedule_ders.Contracts.Api.V1.Responses;

public class AdminRequestListItemDto
{
    public int RequestId { get; set; }
    public int? CourseId { get; set; }
    public string CourseDisplay { get; set; } = string.Empty;
    public string ProfessorName { get; set; } = string.Empty;
    public string ProfessorEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PotentialSiLeaderStatus { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? LastUpdatedAtUtc { get; set; }
}
