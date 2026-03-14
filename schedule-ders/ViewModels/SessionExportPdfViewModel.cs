namespace schedule_ders.ViewModels;

public class SessionExportPdfViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Leader { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string CourseLabel { get; set; } = string.Empty;
    public DateTime GeneratedAtLocal { get; set; }
    public List<SessionExportPdfCourseRowViewModel> Courses { get; set; } = [];
}

public class SessionExportPdfCourseRowViewModel
{
    public int RowNumber { get; set; }
    public string CourseAndSection { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Professor { get; set; } = string.Empty;
    public string Leader { get; set; } = string.Empty;
    public string OfficeHours { get; set; } = string.Empty;
    public List<SessionExportPdfSlotViewModel> Sessions { get; set; } = [];
}

public class SessionExportPdfSlotViewModel
{
    public string Day { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
