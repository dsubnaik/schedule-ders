using System.ComponentModel.DataAnnotations;

namespace schedule_ders.ViewModels;

public class SILeaderAdminIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public List<SILeaderAdminRowViewModel> Leaders { get; set; } = [];

    [Required]
    [StringLength(20)]
    [Display(Name = "A-Number")]
    public string CreateANumber { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "SI Leader Name")]
    public string CreateLeaderName { get; set; } = string.Empty;

    [Display(Name = "Course / Section Assignments")]
    public string CreateCourseAssignments { get; set; } = string.Empty;
}

public class SILeaderAdminRowViewModel
{
    public int SILeaderID { get; set; }
    public string ANumber { get; set; } = string.Empty;
    public string LeaderName { get; set; } = string.Empty;
    public string CoursesTeaching { get; set; } = "-";
    public string SectionNumbers { get; set; } = "-";
    public string CourseAssignmentsInput { get; set; } = string.Empty;
}
