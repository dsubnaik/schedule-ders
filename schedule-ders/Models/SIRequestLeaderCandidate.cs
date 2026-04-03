using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class SIRequestLeaderCandidate
{
    public int SIRequestLeaderCandidateID { get; set; }

    public int SIRequestID { get; set; }

    [Required]
    [StringLength(120)]
    public string CandidateName { get; set; } = string.Empty;

    [StringLength(20)]
    public string CandidateANumber { get; set; } = string.Empty;

    [Required]
    public SILeaderCandidateStatus Status { get; set; } = SILeaderCandidateStatus.Requested;

    public DateTime? LastUpdatedAtUtc { get; set; }

    public SIRequest? SIRequest { get; set; }
}

public enum SILeaderCandidateStatus
{
    Requested = 0,
    Vetted = 1,
    YetToInterview = 2,
    Interviewed = 3,
    Hired = 4,
    NotMovingForward = 5
}
