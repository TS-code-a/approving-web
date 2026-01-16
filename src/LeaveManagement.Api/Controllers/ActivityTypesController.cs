using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Interfaces;
using LeaveManagement.Shared.Common;
using LeaveManagement.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivityTypesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public ActivityTypesController(
        IUnitOfWork unitOfWork,
        IUserService userService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ActivityTypeDto>>>> GetActivityTypes([FromQuery] int? companyId = null)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await _userService.GetUserByIdAsync(userId) ?? throw new InvalidOperationException("User not found");

        var targetCompanyId = companyId ?? user.CompanyId;

        var activityTypes = await _unitOfWork.ActivityTypes
            .Query()
            .Include(a => a.Company)
            .Include(a => a.CustomFields.Where(f => f.IsActive))
            .Where(a => a.IsActive && (a.IsGlobal || a.CompanyId == targetCompanyId))
            .OrderBy(a => a.SortOrder)
            .ToListAsync();

        var dtos = activityTypes.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ActivityTypeDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ActivityTypeDto>>> GetActivityType(int id)
    {
        var activityType = await _unitOfWork.ActivityTypes
            .Query()
            .Include(a => a.Company)
            .Include(a => a.CustomFields.Where(f => f.IsActive))
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activityType == null)
        {
            return NotFound(ApiResponse<ActivityTypeDto>.Fail("Activity type not found"));
        }

        return Ok(ApiResponse<ActivityTypeDto>.Ok(MapToDto(activityType)));
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<ActivityTypeDto>>> CreateActivityType([FromBody] ActivityTypeCreateDto dto)
    {
        var existingCode = await _unitOfWork.ActivityTypes.AnyAsync(a => a.Code == dto.Code);
        if (existingCode)
        {
            return BadRequest(ApiResponse<ActivityTypeDto>.Fail("Activity type code already exists"));
        }

        var activityType = new ActivityType
        {
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            Color = dto.Color,
            Icon = dto.Icon,
            CompanyId = dto.CompanyId,
            IsGlobal = dto.IsGlobal,
            IsActive = true,
            SortOrder = dto.SortOrder,
            RequiresApproval = dto.RequiresApproval,
            ApprovalWorkflow = (ApprovalWorkflowType)dto.ApprovalWorkflow,
            MaxApprovalLevels = dto.MaxApprovalLevels,
            DeductsFromBalance = dto.DeductsFromBalance,
            DefaultAnnualBalance = dto.DefaultAnnualBalance,
            AllowNegativeBalance = dto.AllowNegativeBalance,
            AllowCarryOver = dto.AllowCarryOver,
            MaxCarryOverDays = dto.MaxCarryOverDays,
            TimeTrackingMode = (TimeTrackingMode)dto.TimeTrackingMode,
            DefaultStartTime = dto.DefaultStartTime,
            DefaultEndTime = dto.DefaultEndTime,
            MinDuration = dto.MinDuration,
            MaxDuration = dto.MaxDuration,
            RequiresAttachment = dto.RequiresAttachment,
            NotifyOnSubmit = dto.NotifyOnSubmit,
            NotifyOnApprove = dto.NotifyOnApprove,
            NotifyOnReject = dto.NotifyOnReject,
            NotifyOnCancel = dto.NotifyOnCancel,
            AllowCancellation = dto.AllowCancellation,
            CancellationDeadlineHours = dto.CancellationDeadlineHours,
            RequiresComment = dto.RequiresComment,
            AllowOverlapping = dto.AllowOverlapping,
            MinAdvanceNoticeDays = dto.MinAdvanceNoticeDays,
            CreatedBy = _currentUserService.UserName
        };

        var created = await _unitOfWork.ActivityTypes.AddAsync(activityType);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetActivityType), new { id = created.Id }, ApiResponse<ActivityTypeDto>.Ok(MapToDto(created)));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<ActivityTypeDto>>> UpdateActivityType(int id, [FromBody] ActivityTypeCreateDto dto)
    {
        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(id);
        if (activityType == null)
        {
            return NotFound(ApiResponse<ActivityTypeDto>.Fail("Activity type not found"));
        }

        activityType.Name = dto.Name;
        activityType.Description = dto.Description;
        activityType.Color = dto.Color;
        activityType.Icon = dto.Icon;
        activityType.CompanyId = dto.CompanyId;
        activityType.IsGlobal = dto.IsGlobal;
        activityType.SortOrder = dto.SortOrder;
        activityType.RequiresApproval = dto.RequiresApproval;
        activityType.ApprovalWorkflow = (ApprovalWorkflowType)dto.ApprovalWorkflow;
        activityType.MaxApprovalLevels = dto.MaxApprovalLevels;
        activityType.DeductsFromBalance = dto.DeductsFromBalance;
        activityType.DefaultAnnualBalance = dto.DefaultAnnualBalance;
        activityType.AllowNegativeBalance = dto.AllowNegativeBalance;
        activityType.AllowCarryOver = dto.AllowCarryOver;
        activityType.MaxCarryOverDays = dto.MaxCarryOverDays;
        activityType.TimeTrackingMode = (TimeTrackingMode)dto.TimeTrackingMode;
        activityType.DefaultStartTime = dto.DefaultStartTime;
        activityType.DefaultEndTime = dto.DefaultEndTime;
        activityType.MinDuration = dto.MinDuration;
        activityType.MaxDuration = dto.MaxDuration;
        activityType.RequiresAttachment = dto.RequiresAttachment;
        activityType.NotifyOnSubmit = dto.NotifyOnSubmit;
        activityType.NotifyOnApprove = dto.NotifyOnApprove;
        activityType.NotifyOnReject = dto.NotifyOnReject;
        activityType.NotifyOnCancel = dto.NotifyOnCancel;
        activityType.AllowCancellation = dto.AllowCancellation;
        activityType.CancellationDeadlineHours = dto.CancellationDeadlineHours;
        activityType.RequiresComment = dto.RequiresComment;
        activityType.AllowOverlapping = dto.AllowOverlapping;
        activityType.MinAdvanceNoticeDays = dto.MinAdvanceNoticeDays;
        activityType.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.ActivityTypes.UpdateAsync(activityType);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<ActivityTypeDto>.Ok(MapToDto(activityType)));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse>> DeleteActivityType(int id)
    {
        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(id);
        if (activityType == null)
        {
            return NotFound(ApiResponse.Fail("Activity type not found"));
        }

        // Check if there are any requests using this activity type
        var hasRequests = await _unitOfWork.LeaveRequests.AnyAsync(r => r.ActivityTypeId == id);
        if (hasRequests)
        {
            // Soft delete instead
            activityType.IsActive = false;
            await _unitOfWork.ActivityTypes.UpdateAsync(activityType);
        }
        else
        {
            await _unitOfWork.ActivityTypes.DeleteAsync(activityType);
        }

        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Activity type deleted"));
    }

    [HttpPost("{id}/fields")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<ActivityFieldDto>>> AddCustomField(int id, [FromBody] ActivityFieldDto dto)
    {
        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(id);
        if (activityType == null)
        {
            return NotFound(ApiResponse<ActivityFieldDto>.Fail("Activity type not found"));
        }

        var field = new ActivityField
        {
            ActivityTypeId = id,
            Name = dto.Name,
            Label = dto.Label,
            FieldType = (FieldType)dto.FieldType,
            IsRequired = dto.IsRequired,
            DefaultValue = dto.DefaultValue,
            Placeholder = dto.Placeholder,
            ValidationRegex = dto.ValidationRegex,
            ValidationMessage = dto.ValidationMessage,
            Options = dto.Options,
            SortOrder = dto.SortOrder,
            IsActive = true,
            CreatedBy = _currentUserService.UserName
        };

        var created = await _unitOfWork.ActivityFields.AddAsync(field);
        await _unitOfWork.SaveChangesAsync();

        dto.Id = created.Id;
        dto.ActivityTypeId = id;
        dto.FieldTypeName = field.FieldType.ToString();

        return CreatedAtAction(nameof(GetActivityType), new { id }, ApiResponse<ActivityFieldDto>.Ok(dto));
    }

    private static ActivityTypeDto MapToDto(ActivityType activityType)
    {
        return new ActivityTypeDto
        {
            Id = activityType.Id,
            Name = activityType.Name,
            Code = activityType.Code,
            Description = activityType.Description,
            Color = activityType.Color,
            Icon = activityType.Icon,
            CompanyId = activityType.CompanyId,
            CompanyName = activityType.Company?.Name,
            IsGlobal = activityType.IsGlobal,
            IsActive = activityType.IsActive,
            SortOrder = activityType.SortOrder,
            RequiresApproval = activityType.RequiresApproval,
            ApprovalWorkflow = (int)activityType.ApprovalWorkflow,
            ApprovalWorkflowName = activityType.ApprovalWorkflow.ToString(),
            MaxApprovalLevels = activityType.MaxApprovalLevels,
            DeductsFromBalance = activityType.DeductsFromBalance,
            DefaultAnnualBalance = activityType.DefaultAnnualBalance,
            AllowNegativeBalance = activityType.AllowNegativeBalance,
            AllowCarryOver = activityType.AllowCarryOver,
            MaxCarryOverDays = activityType.MaxCarryOverDays,
            TimeTrackingMode = (int)activityType.TimeTrackingMode,
            TimeTrackingModeName = activityType.TimeTrackingMode.ToString(),
            DefaultStartTime = activityType.DefaultStartTime,
            DefaultEndTime = activityType.DefaultEndTime,
            MinDuration = activityType.MinDuration,
            MaxDuration = activityType.MaxDuration,
            RequiresAttachment = activityType.RequiresAttachment,
            NotifyOnSubmit = activityType.NotifyOnSubmit,
            NotifyOnApprove = activityType.NotifyOnApprove,
            NotifyOnReject = activityType.NotifyOnReject,
            NotifyOnCancel = activityType.NotifyOnCancel,
            AllowCancellation = activityType.AllowCancellation,
            CancellationDeadlineHours = activityType.CancellationDeadlineHours,
            RequiresComment = activityType.RequiresComment,
            AllowOverlapping = activityType.AllowOverlapping,
            MinAdvanceNoticeDays = activityType.MinAdvanceNoticeDays,
            CustomFields = activityType.CustomFields?.Select(f => new ActivityFieldDto
            {
                Id = f.Id,
                ActivityTypeId = f.ActivityTypeId,
                Name = f.Name,
                Label = f.Label,
                FieldType = (int)f.FieldType,
                FieldTypeName = f.FieldType.ToString(),
                IsRequired = f.IsRequired,
                DefaultValue = f.DefaultValue,
                Placeholder = f.Placeholder,
                ValidationRegex = f.ValidationRegex,
                ValidationMessage = f.ValidationMessage,
                Options = f.Options,
                SortOrder = f.SortOrder,
                IsActive = f.IsActive
            }).ToList() ?? new List<ActivityFieldDto>()
        };
    }
}
