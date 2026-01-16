namespace LeaveManagement.Core.Entities;

public class RequestFieldValue : BaseEntity
{
    public int RequestId { get; set; }
    public int ActivityFieldId { get; set; }
    public string? Value { get; set; }

    // Navigation properties
    public virtual LeaveRequest Request { get; set; } = null!;
    public virtual ActivityField ActivityField { get; set; } = null!;
}
