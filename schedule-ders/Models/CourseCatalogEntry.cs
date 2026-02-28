using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class CourseCatalogEntry
{
    public int CourseCatalogEntryID { get; set; }

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
}
