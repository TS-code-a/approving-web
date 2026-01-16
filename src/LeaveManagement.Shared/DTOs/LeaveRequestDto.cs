using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Shared.DTOs;

public class LeaveRequestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserDepartment { get; set; }
    public int ActivityTypeId { get; set; }
    public string ActivityTypeName { get; set; } = string.Empty;
    public string? ActivityTypeColor { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TimeTrackingMode { get; set; }
    public int? HalfDayPeriod { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public decimal TotalDays { get; set; }
    public decimal TotalHours { get; set; }

    public string? Reason { get; set; }
    public string? Comment { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<RequestApprovalDto> Approvals { get; set; } = new();
    public List<RequestFieldValueDto> FieldValues { get; set; } = new();
    public List<RequestCommentDto> Comments { get; set; } = new();
}

public class LeaveRequestCreateDto
{
    [Required]
    public int ActivityTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? HalfDayPeriod { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    public string? Reason { get; set; }
    public string? Comment { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }

    public List<RequestFieldValueCreateDto> FieldValues { get; set; } = new();
}

public class LeaveRequestUpdateDto
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? HalfDayPeriod { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    public string? Reason { get; set; }
    public string? Comment { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }

    public List<RequestFieldValueCreateDto> FieldValues { get; set; } = new();
}

public class RequestApprovalDto
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int ApproverId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public string ApproverEmail { get; set; } = string.Empty;
    public int? ProxyApproverId { get; set; }
    public string? ProxyApproverName { get; set; }
    public int Level { get; set; }
    public int Sequence { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime? ActionDate { get; set; }
    public bool IsRequired { get; set; }
}

public class RequestFieldValueDto
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int ActivityFieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class RequestFieldValueCreateDto
{
    public int ActivityFieldId { get; set; }
    public string? Value { get; set; }
}

public class RequestCommentDto
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApprovalActionDto
{
    public string? Comment { get; set; }
}
