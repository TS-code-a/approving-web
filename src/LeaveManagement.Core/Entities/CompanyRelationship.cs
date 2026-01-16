namespace LeaveManagement.Core.Entities;

public class CompanyRelationship : BaseEntity
{
    public int SourceCompanyId { get; set; }
    public int TargetCompanyId { get; set; }
    public bool CanViewRequests { get; set; } = false;
    public bool CanApproveRequests { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Company SourceCompany { get; set; } = null!;
    public virtual Company TargetCompany { get; set; } = null!;
}
