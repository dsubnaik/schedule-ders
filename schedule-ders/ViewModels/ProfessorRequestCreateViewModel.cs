using System.ComponentModel.DataAnnotations;

namespace schedule_ders.ViewModels;

public class ProfessorRequestCreateViewModel
{
    public int? CourseID { get; set; }

    [StringLength(120)]
    [Display(Name = "Course")]
    public string RequestedCourseName { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Course Name")]
    public string RequestedCourseTitle { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Course Section")]
    public string RequestedCourseSection { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Course Professor")]
    public string RequestedCourseProfessor { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    [Display(Name = "Request Notes")]
    public string RequestNotes { get; set; } = string.Empty;
}
