using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Entities;

public class LeaveRequest : BaseEntity
{
    public int UserId { get; set; }
    public int ActivityTypeId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    // Date/Time details
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeTrackingMode TimeTrackingMode { get; set; }
    public HalfDayPeriod? HalfDayPeriod { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public decimal TotalDays { get; set; }
    public decimal TotalHours { get; set; }

    // Request details
    public string? Reason { get; set; }
    public string? Comment { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }

    // Tracking
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? CancelledByUserId { get; set; }

    // Navigation properties
    public virtual UserProfile User { get; set; } = null!;
    public virtual ActivityType ActivityType { get; set; } = null!;
    public virtual ICollection<RequestApproval> Approvals { get; set; } = new List<RequestApproval>();
    public virtual ICollection<RequestFieldValue> FieldValues { get; set; } = new List<RequestFieldValue>();
    public virtual ICollection<RequestComment> Comments { get; set; } = new List<RequestComment>();
}
