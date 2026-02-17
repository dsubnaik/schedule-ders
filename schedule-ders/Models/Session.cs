using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class Session
{
    public int SessionID { get; set; }

    [Required]
    [StringLength(20)]
    public string Day { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Time { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Location { get; set; } = string.Empty;

    public int CourseID { get; set; }

    public Course? Course { get; set; }
}
