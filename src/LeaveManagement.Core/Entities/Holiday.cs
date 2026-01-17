namespace LeaveManagement.Core.Entities;

public class Holiday : BaseEntity
{
    public int? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsRecurringYearly { get; set; } = false;
    public bool IsHalfDay { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Company? Company { get; set; }
}
