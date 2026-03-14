using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class SIRequestLeaderCandidate
{
    public int SIRequestLeaderCandidateID { get; set; }

    public int SIRequestID { get; set; }

    [Required]
    [StringLength(120)]
    public string CandidateName { get; set; } = string.Empty;

    [Required]
    public SILeaderCandidateStatus Status { get; set; } = SILeaderCandidateStatus.Requested;

    public DateTime? LastUpdatedAtUtc { get; set; }

    public SIRequest? SIRequest { get; set; }
}

public enum SILeaderCandidateStatus
{
    Requested = 0,
    YetToInterview = 1,
    Interviewed = 2,
    Hired = 3
}
