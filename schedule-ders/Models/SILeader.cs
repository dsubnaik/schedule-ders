using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class SILeader
{
    public int SILeaderID { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "A-Number")]
    public string ANumber { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "SI Leader Name")]
    public string LeaderName { get; set; } = string.Empty;

    [Display(Name = "Stored Course Assignments")]
    public string StoredCourseAssignments { get; set; } = string.Empty;

    public ICollection<SILeaderCustomValue> CustomValues { get; set; } = [];
}
