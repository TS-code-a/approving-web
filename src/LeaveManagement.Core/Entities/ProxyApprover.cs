namespace LeaveManagement.Core.Entities;

public class ProxyApprover : BaseEntity
{
    public int OriginalApproverId { get; set; }
    public int ProxyUserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Reason { get; set; }

    // Navigation properties
    public virtual UserProfile OriginalApprover { get; set; } = null!;
    public virtual UserProfile ProxyUser { get; set; } = null!;
}
