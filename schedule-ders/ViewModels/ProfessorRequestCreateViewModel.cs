using System.ComponentModel.DataAnnotations;

namespace schedule_ders.ViewModels;

public class ProfessorRequestCreateViewModel
{
    [Display(Name = "Select Existing Course (Optional)")]
    public int? CourseID { get; set; }

    [StringLength(120)]
    [Display(Name = "Course Name (If Not Listed)")]
    public string RequestedCourseName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Course Section (If Not Listed)")]
    public string RequestedCourseSection { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Course Professor (If Not Listed)")]
    public string RequestedCourseProfessor { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Professor Name")]
    public string ProfessorName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Professor Email")]
    public string ProfessorEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    [Display(Name = "Request Notes")]
    public string RequestNotes { get; set; } = string.Empty;
}
