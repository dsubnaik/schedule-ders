using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class SILeaderCustomValue
{
    public int SILeaderCustomValueId { get; set; }

    public int SILeaderID { get; set; }
    public SILeader SILeader { get; set; } = null!;

    public int SILeaderCustomFieldId { get; set; }
    public SILeaderCustomField SILeaderCustomField { get; set; } = null!;

    [StringLength(500)]
    public string Value { get; set; } = string.Empty;
}
