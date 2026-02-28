namespace schedule_ders.ViewModels;

public class DashboardViewModel
{
    public string Heading { get; set; } = "Dashboard";
    public List<DashboardActionViewModel> Actions { get; set; } = [];
    public List<DashboardMetricViewModel> Metrics { get; set; } = [];
    public List<DashboardActivityViewModel> RecentActivity { get; set; } = [];
    public List<DashboardAttentionItemViewModel> AttentionItems { get; set; } = [];
}

public class DashboardActionViewModel
{
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class DashboardMetricViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Hint { get; set; }
}

public class DashboardActivityViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Meta { get; set; } = string.Empty;
}

public class DashboardAttentionItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? RouteStatus { get; set; }
}
