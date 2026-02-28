namespace schedule_ders.ViewModels;

public class StudentScheduleSearchViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string Professor { get; set; } = string.Empty;
    public bool CanManageFavorites { get; set; }
    public HashSet<int> FavoriteCourseIds { get; set; } = [];
    public List<StudentScheduleRowViewModel> Results { get; set; } = [];
}

public class StudentScheduleRowViewModel
{
    public int CourseID { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseSection { get; set; } = string.Empty;
    public string Professor { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string SILeader { get; set; } = string.Empty;
}
