using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Entities;

public class UserPermission : BaseEntity
{
    public int UserId { get; set; }
    public PermissionType PermissionType { get; set; }
    public int? TargetCompanyId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual UserProfile User { get; set; } = null!;
    public virtual Company? TargetCompany { get; set; }
}
