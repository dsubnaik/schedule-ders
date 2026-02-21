namespace schedule_ders.Contracts.Api.V1.Responses;

public class RequestStatusDto
{
    public int RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? LastUpdatedAtUtc { get; set; }
    public string? AdminNotes { get; set; }
    public int ProgressPercent { get; set; }
}
