namespace LeaveManagement.Core.Entities;

public class UserManager : BaseEntity
{
    public int UserId { get; set; }
    public int ManagerId { get; set; }
    public int Level { get; set; } = 1;
    public bool IsPrimary { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual UserProfile User { get; set; } = null!;
    public virtual UserProfile Manager { get; set; } = null!;
}
