using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Entities;

public class ActivityField : BaseEntity
{
    public int ActivityTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public FieldType FieldType { get; set; }
    public bool IsRequired { get; set; } = false;
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ValidationMessage { get; set; }
    public string? Options { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ActivityType ActivityType { get; set; } = null!;
    public virtual ICollection<RequestFieldValue> FieldValues { get; set; } = new List<RequestFieldValue>();
}
