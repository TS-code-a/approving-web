using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Entities;

public class NotificationTemplate : BaseEntity
{
    public int? ActivityTypeId { get; set; }
    public NotificationTrigger Trigger { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool SendToRequester { get; set; } = true;
    public bool SendToApprovers { get; set; } = false;
    public bool SendToHR { get; set; } = false;

    // Navigation properties
    public virtual ActivityType? ActivityType { get; set; }
}
