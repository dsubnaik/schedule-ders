using System.ComponentModel.DataAnnotations;

namespace schedule_ders.ViewModels;

public class SemestersIndexViewModel
{
    [Required]
    [Display(Name = "Semester Code")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Use a 6-digit semester code like 202601 or 202609.")]
    public string NewSemesterCode { get; set; } = string.Empty;

    public List<SemesterListItemViewModel> Semesters { get; set; } = [];
}

public class SemesterListItemViewModel
{
    public int SemesterId { get; set; }
    public string SemesterCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int CourseCount { get; set; }
    public int SessionCount { get; set; }
    public bool CanDelete { get; set; }
    public string DeleteHint { get; set; } = string.Empty;
}
