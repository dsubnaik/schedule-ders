using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class Course
{
    public int CourseID { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "CRN")]
    public string CourseCrn { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Course")]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Course Name")]
    public string CourseTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [Display(Name = "Section")]
    public string CourseSection { get; set; } = string.Empty;

    [Required]
    [StringLength(5)]
    [RegularExpression("^[MTWRF]+$", ErrorMessage = "Use only M, T, W, R, F (example: MWF).")]
    [Display(Name = "Meeting Day(s)")]
    public string CourseMeetingDays { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    [Display(Name = "Meeting Time")]
    public string CourseMeetingTime { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Professor")]
    public string CourseProfessor { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "SI Leader")]
    public string CourseLeader { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Office Hours Day")]
    public string OfficeHoursDay { get; set; } = string.Empty;

    [StringLength(60)]
    [Display(Name = "Office Hours Time")]
    public string OfficeHoursTime { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Office Hours Location")]
    public string OfficeHoursLocation { get; set; } = string.Empty;

    public List<Session> Sessions { get; set; } = [];
}
