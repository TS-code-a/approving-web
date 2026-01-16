using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Shared.DTOs;

public class ActivityTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public bool IsGlobal { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    // Approval settings
    public bool RequiresApproval { get; set; }
    public int ApprovalWorkflow { get; set; }
    public string ApprovalWorkflowName { get; set; } = string.Empty;
    public int? MaxApprovalLevels { get; set; }

    // Balance settings
    public bool DeductsFromBalance { get; set; }
    public decimal? DefaultAnnualBalance { get; set; }
    public bool AllowNegativeBalance { get; set; }
    public bool AllowCarryOver { get; set; }
    public decimal? MaxCarryOverDays { get; set; }

    // Time tracking settings
    public int TimeTrackingMode { get; set; }
    public string TimeTrackingModeName { get; set; } = string.Empty;
    public TimeSpan? DefaultStartTime { get; set; }
    public TimeSpan? DefaultEndTime { get; set; }
    public decimal? MinDuration { get; set; }
    public decimal? MaxDuration { get; set; }
    public bool RequiresAttachment { get; set; }

    // Notification settings
    public bool NotifyOnSubmit { get; set; }
    public bool NotifyOnApprove { get; set; }
    public bool NotifyOnReject { get; set; }
    public bool NotifyOnCancel { get; set; }

    // Advanced settings
    public bool AllowCancellation { get; set; }
    public int? CancellationDeadlineHours { get; set; }
    public bool RequiresComment { get; set; }
    public bool AllowOverlapping { get; set; }
    public int? MinAdvanceNoticeDays { get; set; }

    // Custom fields
    public List<ActivityFieldDto> CustomFields { get; set; } = new();
}

public class ActivityTypeCreateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int? CompanyId { get; set; }
    public bool IsGlobal { get; set; }
    public int SortOrder { get; set; }

    public bool RequiresApproval { get; set; } = true;
    public int ApprovalWorkflow { get; set; }
    public int? MaxApprovalLevels { get; set; }

    public bool DeductsFromBalance { get; set; } = true;
    public decimal? DefaultAnnualBalance { get; set; }
    public bool AllowNegativeBalance { get; set; }
    public bool AllowCarryOver { get; set; }
    public decimal? MaxCarryOverDays { get; set; }

    public int TimeTrackingMode { get; set; }
    public TimeSpan? DefaultStartTime { get; set; }
    public TimeSpan? DefaultEndTime { get; set; }
    public decimal? MinDuration { get; set; }
    public decimal? MaxDuration { get; set; }
    public bool RequiresAttachment { get; set; }

    public bool NotifyOnSubmit { get; set; } = true;
    public bool NotifyOnApprove { get; set; } = true;
    public bool NotifyOnReject { get; set; } = true;
    public bool NotifyOnCancel { get; set; }

    public bool AllowCancellation { get; set; } = true;
    public int? CancellationDeadlineHours { get; set; }
    public bool RequiresComment { get; set; }
    public bool AllowOverlapping { get; set; }
    public int? MinAdvanceNoticeDays { get; set; }
}

public class ActivityFieldDto
{
    public int Id { get; set; }
    public int ActivityTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int FieldType { get; set; }
    public string FieldTypeName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ValidationMessage { get; set; }
    public string? Options { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
