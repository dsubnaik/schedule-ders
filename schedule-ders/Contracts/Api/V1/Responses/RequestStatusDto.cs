namespace schedule_ders.Contracts.Api.V1.Responses;

public class RequestStatusDto
{
    public int RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PotentialSiLeaderStatus { get; set; } = string.Empty;
    public string? PotentialSiLeaderName { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? LastUpdatedAtUtc { get; set; }
    public string? AdminNotes { get; set; }
    public int ProgressPercent { get; set; }
    public int PotentialSiLeaderProgressPercent { get; set; }
    public List<LeaderCandidateStatusDto> LeaderCandidates { get; set; } = [];
}

public class LeaderCandidateStatusDto
{
    public int CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
}
