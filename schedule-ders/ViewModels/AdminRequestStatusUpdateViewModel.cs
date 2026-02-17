using System.ComponentModel.DataAnnotations;
using schedule_ders.Models;

namespace schedule_ders.ViewModels;

public class AdminRequestStatusUpdateViewModel
{
    public int SIRequestID { get; set; }
    public int? CourseID { get; set; }
    public string CourseDisplay { get; set; } = string.Empty;
    public string ProfessorName { get; set; } = string.Empty;
    public string ProfessorEmail { get; set; } = string.Empty;
    public string RequestNotes { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }

    [Required]
    public SIRequestStatus Status { get; set; }

    [StringLength(1000)]
    [Display(Name = "Admin Notes")]
    public string AdminNotes { get; set; } = string.Empty;
}
