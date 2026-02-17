using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class StudentFavoriteCourse
{
    public int StudentFavoriteCourseID { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int CourseID { get; set; }

    public Course? Course { get; set; }
}
