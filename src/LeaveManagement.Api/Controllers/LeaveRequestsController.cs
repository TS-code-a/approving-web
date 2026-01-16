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
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _requestService;
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LeaveRequestsController(
        ILeaveRequestService requestService,
        IUserService userService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _requestService = requestService;
        _userService = userService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<LeaveRequestDto>>>> GetRequests(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? status = null,
        [FromQuery] int? activityTypeId = null,
        [FromQuery] int? year = null)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var targetYear = year ?? DateTime.UtcNow.Year;

        var requests = await _unitOfWork.LeaveRequests
            .Query()
            .Include(r => r.User)
            .Include(r => r.ActivityType)
            .Include(r => r.Approvals)
                .ThenInclude(a => a.Approver)
            .Where(r => r.UserId == userId && r.StartDate.Year == targetYear)
            .Where(r => status == null || (int)r.Status == status)
            .Where(r => activityTypeId == null || r.ActivityTypeId == activityTypeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var dtos = requests.Select(MapToDto).ToList();

        var result = new PagedResult<LeaveRequestDto>(
            dtos.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToList(),
            dtos.Count,
            query.PageNumber,
            query.PageSize);

        return Ok(ApiResponse<PagedResult<LeaveRequestDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> GetRequest(int id)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var canView = await _userService.CanViewRequestAsync(userId, id);
        if (!canView)
        {
            return Forbid();
        }

        var request = await _unitOfWork.LeaveRequests
            .Query()
            .Include(r => r.User)
            .Include(r => r.ActivityType)
            .Include(r => r.Approvals)
                .ThenInclude(a => a.Approver)
            .Include(r => r.FieldValues)
                .ThenInclude(f => f.ActivityField)
            .Include(r => r.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return NotFound(ApiResponse<LeaveRequestDto>.Fail("Request not found"));
        }

        return Ok(ApiResponse<LeaveRequestDto>.Ok(MapToDto(request)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> CreateRequest([FromBody] LeaveRequestCreateDto dto)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var request = new LeaveRequest
        {
            UserId = userId,
            ActivityTypeId = dto.ActivityTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            HalfDayPeriod = dto.HalfDayPeriod.HasValue ? (HalfDayPeriod)dto.HalfDayPeriod : null,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Reason = dto.Reason,
            Comment = dto.Comment,
            AttachmentUrl = dto.AttachmentUrl,
            AttachmentName = dto.AttachmentName
        };

        var createdRequest = await _requestService.CreateRequestAsync(request);

        // Add custom field values
        if (dto.FieldValues.Any())
        {
            foreach (var fieldValue in dto.FieldValues)
            {
                await _unitOfWork.RequestFieldValues.AddAsync(new RequestFieldValue
                {
                    RequestId = createdRequest.Id,
                    ActivityFieldId = fieldValue.ActivityFieldId,
                    Value = fieldValue.Value
                });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        var result = await GetRequestById(createdRequest.Id);
        return CreatedAtAction(nameof(GetRequest), new { id = createdRequest.Id }, ApiResponse<LeaveRequestDto>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> UpdateRequest(int id, [FromBody] LeaveRequestUpdateDto dto)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<LeaveRequestDto>.Fail("Request not found"));
        }

        if (request.UserId != userId)
        {
            return Forbid();
        }

        if (request.Status != RequestStatus.Draft && request.Status != RequestStatus.RevisionRequested)
        {
            return BadRequest(ApiResponse<LeaveRequestDto>.Fail("Only draft or revision-requested requests can be updated"));
        }

        request.StartDate = dto.StartDate;
        request.EndDate = dto.EndDate;
        request.HalfDayPeriod = dto.HalfDayPeriod.HasValue ? (HalfDayPeriod)dto.HalfDayPeriod : null;
        request.StartTime = dto.StartTime;
        request.EndTime = dto.EndTime;
        request.Reason = dto.Reason;
        request.Comment = dto.Comment;
        request.AttachmentUrl = dto.AttachmentUrl;
        request.AttachmentName = dto.AttachmentName;

        await _unitOfWork.LeaveRequests.UpdateAsync(request);
        await _unitOfWork.SaveChangesAsync();

        var result = await GetRequestById(id);
        return Ok(ApiResponse<LeaveRequestDto>.Ok(result));
    }

    [HttpPost("{id}/submit")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> SubmitRequest(int id)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var request = await _requestService.SubmitRequestAsync(id, userId);
        var result = await GetRequestById(request.Id);

        return Ok(ApiResponse<LeaveRequestDto>.Ok(result, "Request submitted successfully"));
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> ApproveRequest(int id, [FromBody] ApprovalActionDto dto)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var canApprove = await _userService.CanApproveRequestAsync(userId, id);
        if (!canApprove)
        {
            return Forbid();
        }

        var request = await _requestService.ApproveRequestAsync(id, userId, dto.Comment);
        var result = await GetRequestById(request.Id);

        return Ok(ApiResponse<LeaveRequestDto>.Ok(result, "Request approved successfully"));
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> RejectRequest(int id, [FromBody] ApprovalActionDto dto)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var canApprove = await _userService.CanApproveRequestAsync(userId, id);
        if (!canApprove)
        {
            return Forbid();
        }

        var request = await _requestService.RejectRequestAsync(id, userId, dto.Comment);
        var result = await GetRequestById(request.Id);

        return Ok(ApiResponse<LeaveRequestDto>.Ok(result, "Request rejected"));
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> CancelRequest(int id, [FromBody] ApprovalActionDto dto)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<LeaveRequestDto>.Fail("Request not found"));
        }

        // User can cancel their own request, or admin can cancel any request
        var isAdmin = await _userService.HasPermissionAsync(userId, PermissionType.SystemAdmin);
        if (request.UserId != userId && !isAdmin)
        {
            return Forbid();
        }

        var cancelledRequest = await _requestService.CancelRequestAsync(id, userId, dto.Comment);
        var result = await GetRequestById(cancelledRequest.Id);

        return Ok(ApiResponse<LeaveRequestDto>.Ok(result, "Request cancelled"));
    }

    [HttpPost("{id}/request-revision")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> RequestRevision(int id, [FromBody] ApprovalActionDto dto)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest(ApiResponse<LeaveRequestDto>.Fail("Comment is required for revision request"));
        }

        var canApprove = await _userService.CanApproveRequestAsync(userId, id);
        if (!canApprove)
        {
            return Forbid();
        }

        var request = await _requestService.RequestRevisionAsync(id, userId, dto.Comment);
        var result = await GetRequestById(request.Id);

        return Ok(ApiResponse<LeaveRequestDto>.Ok(result, "Revision requested"));
    }

    [HttpGet("pending-approvals")]
    public async Task<ActionResult<ApiResponse<List<LeaveRequestDto>>>> GetPendingApprovals()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var requests = await _requestService.GetPendingApprovalsAsync(userId);

        var fullRequests = new List<LeaveRequest>();
        foreach (var r in requests)
        {
            var fullRequest = await _unitOfWork.LeaveRequests
                .Query()
                .Include(x => x.User)
                .Include(x => x.ActivityType)
                .Include(x => x.Approvals)
                    .ThenInclude(a => a.Approver)
                .FirstOrDefaultAsync(x => x.Id == r.Id);

            if (fullRequest != null)
            {
                fullRequests.Add(fullRequest);
            }
        }

        var dtos = fullRequests.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<LeaveRequestDto>>.Ok(dtos));
    }

    [HttpGet("team")]
    public async Task<ActionResult<ApiResponse<List<LeaveRequestDto>>>> GetTeamRequests([FromQuery] int? year = null)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var targetYear = year ?? DateTime.UtcNow.Year;

        var requests = await _requestService.GetTeamRequestsAsync(userId);

        var fullRequests = new List<LeaveRequest>();
        foreach (var r in requests.Where(r => r.StartDate.Year == targetYear))
        {
            var fullRequest = await _unitOfWork.LeaveRequests
                .Query()
                .Include(x => x.User)
                .Include(x => x.ActivityType)
                .Include(x => x.Approvals)
                    .ThenInclude(a => a.Approver)
                .FirstOrDefaultAsync(x => x.Id == r.Id);

            if (fullRequest != null)
            {
                fullRequests.Add(fullRequest);
            }
        }

        var dtos = fullRequests.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<LeaveRequestDto>>.Ok(dtos));
    }

    [HttpGet("calculate-days")]
    public async Task<ActionResult<ApiResponse<decimal>>> CalculateDays(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int timeTrackingMode = 0)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await _userService.GetUserByIdAsync(userId) ?? throw new InvalidOperationException("User not found");

        var days = await _requestService.CalculateDaysAsync(
            startDate,
            endDate,
            (TimeTrackingMode)timeTrackingMode,
            user.CompanyId);

        return Ok(ApiResponse<decimal>.Ok(days));
    }

    private async Task<LeaveRequestDto> GetRequestById(int id)
    {
        var request = await _unitOfWork.LeaveRequests
            .Query()
            .Include(r => r.User)
            .Include(r => r.ActivityType)
            .Include(r => r.Approvals)
                .ThenInclude(a => a.Approver)
            .Include(r => r.FieldValues)
                .ThenInclude(f => f.ActivityField)
            .Include(r => r.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        return MapToDto(request!);
    }

    private static LeaveRequestDto MapToDto(LeaveRequest request)
    {
        return new LeaveRequestDto
        {
            Id = request.Id,
            UserId = request.UserId,
            UserName = request.User?.FullName ?? string.Empty,
            UserEmail = request.User?.Email ?? string.Empty,
            UserDepartment = request.User?.Department,
            ActivityTypeId = request.ActivityTypeId,
            ActivityTypeName = request.ActivityType?.Name ?? string.Empty,
            ActivityTypeColor = request.ActivityType?.Color,
            RequestNumber = request.RequestNumber,
            Status = (int)request.Status,
            StatusName = request.Status.ToString(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TimeTrackingMode = (int)request.TimeTrackingMode,
            HalfDayPeriod = request.HalfDayPeriod.HasValue ? (int)request.HalfDayPeriod : null,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TotalDays = request.TotalDays,
            TotalHours = request.TotalHours,
            Reason = request.Reason,
            Comment = request.Comment,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentName = request.AttachmentName,
            SubmittedAt = request.SubmittedAt,
            ProcessedAt = request.ProcessedAt,
            CancellationReason = request.CancellationReason,
            CancelledAt = request.CancelledAt,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            Approvals = request.Approvals?.Select(a => new RequestApprovalDto
            {
                Id = a.Id,
                RequestId = a.RequestId,
                ApproverId = a.ApproverId,
                ApproverName = a.Approver?.FullName ?? string.Empty,
                ApproverEmail = a.Approver?.Email ?? string.Empty,
                Level = a.Level,
                Sequence = a.Sequence,
                Status = (int)a.Status,
                StatusName = a.Status.ToString(),
                Comment = a.Comment,
                ActionDate = a.ActionDate,
                IsRequired = a.IsRequired
            }).ToList() ?? new List<RequestApprovalDto>(),
            FieldValues = request.FieldValues?.Select(f => new RequestFieldValueDto
            {
                Id = f.Id,
                RequestId = f.RequestId,
                ActivityFieldId = f.ActivityFieldId,
                FieldName = f.ActivityField?.Name ?? string.Empty,
                FieldLabel = f.ActivityField?.Label ?? string.Empty,
                Value = f.Value
            }).ToList() ?? new List<RequestFieldValueDto>(),
            Comments = request.Comments?.Select(c => new RequestCommentDto
            {
                Id = c.Id,
                RequestId = c.RequestId,
                UserId = c.UserId,
                UserName = c.User?.FullName ?? string.Empty,
                Comment = c.Comment,
                IsInternal = c.IsInternal,
                CreatedAt = c.CreatedAt
            }).ToList() ?? new List<RequestCommentDto>()
        };
    }
}
