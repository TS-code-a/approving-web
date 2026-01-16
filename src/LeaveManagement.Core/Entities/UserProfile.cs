using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Entities;

public class UserProfile : BaseEntity
{
    public string ExternalUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public int CompanyId { get; set; }
    public bool IsActive { get; set; } = true;
    public ApprovalLogicType ApprovalLogic { get; set; } = ApprovalLogicType.AnyManager;
    public DateTime? HireDate { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<UserManager> ManagerRelationships { get; set; } = new List<UserManager>();
    public virtual ICollection<UserManager> SubordinateRelationships { get; set; } = new List<UserManager>();
    public virtual ICollection<UserPermission> Permissions { get; set; } = new List<UserPermission>();
    public virtual ICollection<UserBalance> Balances { get; set; } = new List<UserBalance>();
    public virtual ICollection<LeaveRequest> Requests { get; set; } = new List<LeaveRequest>();
    public virtual ICollection<RequestApproval> Approvals { get; set; } = new List<RequestApproval>();
    public virtual ICollection<ProxyApprover> ProxyApproversFor { get; set; } = new List<ProxyApprover>();
    public virtual ICollection<ProxyApprover> ActingAsProxy { get; set; } = new List<ProxyApprover>();
}
