using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Entities;

public class ActivityType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int? CompanyId { get; set; }
    public bool IsGlobal { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    // Approval settings
    public bool RequiresApproval { get; set; } = true;
    public ApprovalWorkflowType ApprovalWorkflow { get; set; } = ApprovalWorkflowType.SingleLevel;
    public int? MaxApprovalLevels { get; set; }

    // Balance settings
    public bool DeductsFromBalance { get; set; } = true;
    public decimal? DefaultAnnualBalance { get; set; }
    public bool AllowNegativeBalance { get; set; } = false;
    public bool AllowCarryOver { get; set; } = false;
    public decimal? MaxCarryOverDays { get; set; }

    // Time tracking settings
    public TimeTrackingMode TimeTrackingMode { get; set; } = TimeTrackingMode.FullDay;
    public TimeSpan? DefaultStartTime { get; set; }
    public TimeSpan? DefaultEndTime { get; set; }
    public decimal? MinDuration { get; set; }
    public decimal? MaxDuration { get; set; }
    public bool RequiresAttachment { get; set; } = false;

    // Notification settings
    public bool NotifyOnSubmit { get; set; } = true;
    public bool NotifyOnApprove { get; set; } = true;
    public bool NotifyOnReject { get; set; } = true;
    public bool NotifyOnCancel { get; set; } = false;

    // Advanced settings
    public bool AllowCancellation { get; set; } = true;
    public int? CancellationDeadlineHours { get; set; }
    public bool RequiresComment { get; set; } = false;
    public bool AllowOverlapping { get; set; } = false;
    public int? MinAdvanceNoticeDays { get; set; }

    // Navigation properties
    public virtual Company? Company { get; set; }
    public virtual ICollection<ActivityField> CustomFields { get; set; } = new List<ActivityField>();
    public virtual ICollection<LeaveRequest> Requests { get; set; } = new List<LeaveRequest>();
    public virtual ICollection<UserBalance> Balances { get; set; } = new List<UserBalance>();
    public virtual ICollection<NotificationTemplate> NotificationTemplates { get; set; } = new List<NotificationTemplate>();
}
