namespace LeaveManagement.Core.Entities;

public class UserBalance : BaseEntity
{
    public int UserId { get; set; }
    public int ActivityTypeId { get; set; }
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal CarriedOverDays { get; set; }
    public decimal AdjustmentDays { get; set; }

    public decimal AvailableDays => TotalDays + CarriedOverDays + AdjustmentDays - UsedDays - PendingDays;

    // Navigation properties
    public virtual UserProfile User { get; set; } = null!;
    public virtual ActivityType ActivityType { get; set; } = null!;
}
