using System.ComponentModel.DataAnnotations;
using schedule_ders.Models;

namespace schedule_ders.Contracts.Api.V1.Requests;

public class UpdateRequestStatusDto
{
    [Required]
    public SIRequestStatus Status { get; set; }

    public SILeaderReviewStatus? PotentialSiLeaderStatus { get; set; }

    [StringLength(1000)]
    public string? AdminNotes { get; set; }
}
