using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Entities;

public class RequestApproval : BaseEntity
{
    public int RequestId { get; set; }
    public int ApproverId { get; set; }
    public int? ProxyApproverId { get; set; }
    public int Level { get; set; }
    public int Sequence { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? Comment { get; set; }
    public DateTime? ActionDate { get; set; }
    public bool IsRequired { get; set; } = true;

    // Navigation properties
    public virtual LeaveRequest Request { get; set; } = null!;
    public virtual UserProfile Approver { get; set; } = null!;
    public virtual UserProfile? ProxyApprover { get; set; }
}
