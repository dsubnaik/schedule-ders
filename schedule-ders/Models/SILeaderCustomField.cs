using System.ComponentModel.DataAnnotations;

namespace schedule_ders.Models;

public class SILeaderCustomField
{
    public int SILeaderCustomFieldId { get; set; }

    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public ICollection<SILeaderCustomValue> Values { get; set; } = [];
}
