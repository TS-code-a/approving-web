namespace LeaveManagement.Core.Entities;

public class RequestComment : BaseEntity
{
    public int RequestId { get; set; }
    public int UserId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;

    // Navigation properties
    public virtual LeaveRequest Request { get; set; } = null!;
    public virtual UserProfile User { get; set; } = null!;
}
