using System.ComponentModel.DataAnnotations;

namespace schedule_ders.ViewModels;

public class SILeaderAdminIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public List<SILeaderAdminRowViewModel> Leaders { get; set; } = [];
    public List<SILeaderCustomFieldViewModel> CustomFields { get; set; } = [];
    public bool ShowANumber { get; set; } = true;
    public bool ShowLeaderName { get; set; } = true;
    public bool ShowCoursesTeaching { get; set; } = true;
    public bool ShowSectionNumbers { get; set; } = true;
    public string ExportTitle { get; set; } = "SI Leaders";
    public string ExportANumberLabel { get; set; } = "A-Number";
    public string ExportLeaderNameLabel { get; set; } = "SI Leader Name";
    public string ExportCoursesTeachingLabel { get; set; } = "Courses Teaching";
    public string ExportSectionNumbersLabel { get; set; } = "Section Numbers";

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

    [StringLength(80)]
    [Display(Name = "New Column Name")]
    public string CreateCustomFieldName { get; set; } = string.Empty;

    public Dictionary<int, string> CreateCustomFieldValues { get; set; } = [];
}

public class SILeaderAdminRowViewModel
{
    public int SILeaderID { get; set; }
    public string ANumber { get; set; } = string.Empty;
    public string LeaderName { get; set; } = string.Empty;
    public string CoursesTeaching { get; set; } = "-";
    public string SectionNumbers { get; set; } = "-";
    public string CourseAssignmentsInput { get; set; } = string.Empty;
    public Dictionary<int, string> CustomFieldValues { get; set; } = [];
}

public class SILeaderCustomFieldViewModel
{
    public int SILeaderCustomFieldId { get; set; }
    public string Name { get; set; } = string.Empty;
}
