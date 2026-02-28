using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Contracts.Api.V1.Requests;

public class CreateSiRequestDto : IValidatableObject
{
    public int? CourseId { get; set; }

    [StringLength(120)]
    public string? RequestedCourseName { get; set; }

    [StringLength(200)]
    public string? RequestedCourseTitle { get; set; }

    [StringLength(30)]
    public string? RequestedCourseSection { get; set; }

    [StringLength(120)]
    public string? RequestedCourseProfessor { get; set; }

    [StringLength(120)]
    public string? ProfessorName { get; set; }

    [StringLength(256)]
    public string? ProfessorEmail { get; set; }

    [Required]
    [StringLength(1000)]
    public string RequestNotes { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CourseId.HasValue)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(RequestedCourseName))
        {
            yield return new ValidationResult("RequestedCourseName is required when CourseId is not provided.", [nameof(RequestedCourseName)]);
        }

        if (string.IsNullOrWhiteSpace(RequestedCourseSection))
        {
            yield return new ValidationResult("RequestedCourseSection is required when CourseId is not provided.", [nameof(RequestedCourseSection)]);
        }

        if (string.IsNullOrWhiteSpace(RequestedCourseTitle))
        {
            yield return new ValidationResult("RequestedCourseTitle is required when CourseId is not provided.", [nameof(RequestedCourseTitle)]);
        }

    }
}
