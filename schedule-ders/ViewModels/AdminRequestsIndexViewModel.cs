using schedule_ders.Models;

namespace schedule_ders.ViewModels;

public class AdminRequestsIndexViewModel
{
    public string CurrentStatus { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public List<SIRequest> Requests { get; set; } = [];
}
