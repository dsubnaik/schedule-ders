namespace schedule_ders.Contracts.Api.V1.Responses;

public class SiRequestSummaryDto
{
    public int RequestId { get; set; }
    public string CourseDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
}
