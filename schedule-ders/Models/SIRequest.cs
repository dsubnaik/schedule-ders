using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class SIRequest
{
    public int SIRequestID { get; set; }

    public int? CourseID { get; set; }

    [StringLength(120)]
    [Display(Name = "Requested Course Name")]
    public string RequestedCourseName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Requested Course Section")]
    public string RequestedCourseSection { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Requested Course Professor")]
    public string RequestedCourseProfessor { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Professor Name")]
    public string ProfessorName { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    [EmailAddress]
    [Display(Name = "Professor Email")]
    public string ProfessorEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    [Display(Name = "Request Notes")]
    public string RequestNotes { get; set; } = string.Empty;

    [Display(Name = "Submitted On")]
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Display(Name = "Status")]
    public SIRequestStatus Status { get; set; } = SIRequestStatus.Pending;

    [StringLength(1000)]
    [Display(Name = "Admin Notes")]
    public string AdminNotes { get; set; } = string.Empty;

    [Display(Name = "Last Updated")]
    public DateTime? LastUpdatedAtUtc { get; set; }

    public Course? Course { get; set; }
}

public enum SIRequestStatus
{
    Pending = 0,
    UnderReview = 1,
    Approved = 2,
    Denied = 3
}
