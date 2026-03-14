using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class Semester
{
    public int SemesterId { get; set; }

    [Required]
    [StringLength(6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Use a 6-digit semester code like 202601 or 202609.")]
    [Display(Name = "Semester Code")]
    public string SemesterCode { get; set; } = string.Empty;

    public List<Course> Courses { get; set; } = [];
}
