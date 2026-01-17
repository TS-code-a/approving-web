using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Interfaces;

namespace LeaveManagement.Core.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApprovalWorkflowService _workflowService;
    private readonly IBalanceService _balanceService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public LeaveRequestService(
        IUnitOfWork unitOfWork,
        IApprovalWorkflowService workflowService,
        IBalanceService balanceService,
        INotificationService notificationService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _workflowService = workflowService;
        _balanceService = balanceService;
        _notificationService = notificationService;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<LeaveRequest> CreateRequestAsync(LeaveRequest request, CancellationToken cancellationToken = default)
    {
        request.RequestNumber = await GenerateRequestNumberAsync(cancellationToken);
        request.Status = RequestStatus.Draft;
        request.CreatedBy = _currentUserService.UserName;

        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(request.ActivityTypeId, cancellationToken)
            ?? throw new InvalidOperationException("Activity type not found");

        request.TimeTrackingMode = activityType.TimeTrackingMode;

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        request.TotalDays = await CalculateDaysAsync(
            request.StartDate,
            request.EndDate,
            request.TimeTrackingMode,
            user.CompanyId,
            cancellationToken);

        if (activityType.DeductsFromBalance)
        {
            var hasSufficientBalance = await _balanceService.HasSufficientBalanceAsync(
                request.UserId,
                request.ActivityTypeId,
                request.StartDate.Year,
                request.TotalDays,
                cancellationToken);

            if (!hasSufficientBalance && !activityType.AllowNegativeBalance)
            {
                throw new InvalidOperationException("Insufficient balance for this request");
            }
        }

        var overlapping = await HasOverlappingRequestAsync(
            request.UserId,
            request.StartDate,
            request.EndDate,
            null,
            cancellationToken);

        if (overlapping && !activityType.AllowOverlapping)
        {
            throw new InvalidOperationException("You have an overlapping request for this period");
        }

        var createdRequest = await _unitOfWork.LeaveRequests.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("LeaveRequest", createdRequest.Id, "Created", null, createdRequest, cancellationToken);

        return createdRequest;
    }

    public async Task<LeaveRequest> SubmitRequestAsync(int requestId, int userId, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found");

        if (request.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only submit your own requests");
        }

        if (request.Status != RequestStatus.Draft && request.Status != RequestStatus.RevisionRequested)
        {
            throw new InvalidOperationException("Only draft or revision-requested requests can be submitted");
        }

        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(request.ActivityTypeId, cancellationToken)
            ?? throw new InvalidOperationException("Activity type not found");

        var oldStatus = request.Status;
        request.Status = RequestStatus.Pending;
        request.SubmittedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = _currentUserService.UserName;

        if (activityType.ApprovalWorkflow == ApprovalWorkflowType.AutoApprove || !activityType.RequiresApproval)
        {
            request.Status = RequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;

            if (activityType.DeductsFromBalance)
            {
                await _balanceService.DeductBalanceAsync(
                    request.UserId,
                    request.ActivityTypeId,
                    request.StartDate.Year,
                    request.TotalDays,
                    cancellationToken);
            }
        }
        else
        {
            var approvals = await _workflowService.GenerateApprovalChainAsync(request, cancellationToken);
            foreach (var approval in approvals)
            {
                await _unitOfWork.RequestApprovals.AddAsync(approval, cancellationToken);
            }

            if (activityType.DeductsFromBalance)
            {
                await _balanceService.AddPendingDaysAsync(
                    request.UserId,
                    request.ActivityTypeId,
                    request.StartDate.Year,
                    request.TotalDays,
                    cancellationToken);
            }
        }

        await _unitOfWork.LeaveRequests.UpdateAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("LeaveRequest", request.Id, "Submitted",
            new { Status = oldStatus }, new { Status = request.Status }, cancellationToken);

        await _notificationService.SendNotificationAsync(request, NotificationTrigger.OnSubmit, cancellationToken);

        return request;
    }

    public async Task<LeaveRequest> ApproveRequestAsync(int requestId, int approverId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found");

        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be approved");
        }

        var approval = (await _unitOfWork.RequestApprovals
            .FindAsync(a => a.RequestId == requestId &&
                           a.ApproverId == approverId &&
                           a.Status == ApprovalStatus.Pending, cancellationToken))
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No pending approval found for this user");

        approval.Status = ApprovalStatus.Approved;
        approval.Comment = comment;
        approval.ActionDate = DateTime.UtcNow;
        approval.UpdatedAt = DateTime.UtcNow;
        approval.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.RequestApprovals.UpdateAsync(approval, cancellationToken);

        var isComplete = await _workflowService.IsApprovalCompleteAsync(requestId, cancellationToken);

        if (isComplete)
        {
            var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(request.ActivityTypeId, cancellationToken)!;

            request.Status = RequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.UpdatedBy = _currentUserService.UserName;

            if (activityType?.DeductsFromBalance == true)
            {
                await _balanceService.RemovePendingDaysAsync(
                    request.UserId,
                    request.ActivityTypeId,
                    request.StartDate.Year,
                    request.TotalDays,
                    cancellationToken);

                await _balanceService.DeductBalanceAsync(
                    request.UserId,
                    request.ActivityTypeId,
                    request.StartDate.Year,
                    request.TotalDays,
                    cancellationToken);
            }

            await _unitOfWork.LeaveRequests.UpdateAsync(request, cancellationToken);
            await _notificationService.SendNotificationAsync(request, NotificationTrigger.OnApprove, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("RequestApproval", approval.Id, "Approved",
            null, new { approval.Status, approval.Comment }, cancellationToken);

        return request;
    }

    public async Task<LeaveRequest> RejectRequestAsync(int requestId, int approverId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found");

        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be rejected");
        }

        var approval = (await _unitOfWork.RequestApprovals
            .FindAsync(a => a.RequestId == requestId &&
                           a.ApproverId == approverId &&
                           a.Status == ApprovalStatus.Pending, cancellationToken))
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No pending approval found for this user");

        approval.Status = ApprovalStatus.Rejected;
        approval.Comment = comment;
        approval.ActionDate = DateTime.UtcNow;
        approval.UpdatedAt = DateTime.UtcNow;
        approval.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.RequestApprovals.UpdateAsync(approval, cancellationToken);

        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(request.ActivityTypeId, cancellationToken);

        request.Status = RequestStatus.Rejected;
        request.ProcessedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = _currentUserService.UserName;

        if (activityType?.DeductsFromBalance == true)
        {
            await _balanceService.RemovePendingDaysAsync(
                request.UserId,
                request.ActivityTypeId,
                request.StartDate.Year,
                request.TotalDays,
                cancellationToken);
        }

        await _unitOfWork.LeaveRequests.UpdateAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("RequestApproval", approval.Id, "Rejected",
            null, new { approval.Status, approval.Comment }, cancellationToken);

        await _notificationService.SendNotificationAsync(request, NotificationTrigger.OnReject, cancellationToken);

        return request;
    }

    public async Task<LeaveRequest> CancelRequestAsync(int requestId, int userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found");

        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(request.ActivityTypeId, cancellationToken)
            ?? throw new InvalidOperationException("Activity type not found");

        if (!activityType.AllowCancellation)
        {
            throw new InvalidOperationException("This activity type does not allow cancellation");
        }

        if (request.Status == RequestStatus.Cancelled)
        {
            throw new InvalidOperationException("Request is already cancelled");
        }

        var oldStatus = request.Status;
        request.Status = RequestStatus.Cancelled;
        request.CancellationReason = reason;
        request.CancelledAt = DateTime.UtcNow;
        request.CancelledByUserId = userId;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = _currentUserService.UserName;

        if (oldStatus == RequestStatus.Pending && activityType.DeductsFromBalance)
        {
            await _balanceService.RemovePendingDaysAsync(
                request.UserId,
                request.ActivityTypeId,
                request.StartDate.Year,
                request.TotalDays,
                cancellationToken);
        }
        else if (oldStatus == RequestStatus.Approved && activityType.DeductsFromBalance)
        {
            await _balanceService.RestoreBalanceAsync(
                request.UserId,
                request.ActivityTypeId,
                request.StartDate.Year,
                request.TotalDays,
                cancellationToken);
        }

        await _unitOfWork.LeaveRequests.UpdateAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("LeaveRequest", request.Id, "Cancelled",
            new { Status = oldStatus }, new { Status = request.Status, request.CancellationReason }, cancellationToken);

        await _notificationService.SendNotificationAsync(request, NotificationTrigger.OnCancel, cancellationToken);

        return request;
    }

    public async Task<LeaveRequest> RequestRevisionAsync(int requestId, int approverId, string comment, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found");

        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be returned for revision");
        }

        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(request.ActivityTypeId, cancellationToken);

        request.Status = RequestStatus.RevisionRequested;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = _currentUserService.UserName;

        var requestComment = new RequestComment
        {
            RequestId = requestId,
            UserId = approverId,
            Comment = comment,
            IsInternal = false,
            CreatedBy = _currentUserService.UserName
        };

        await _unitOfWork.RequestComments.AddAsync(requestComment, cancellationToken);

        if (activityType?.DeductsFromBalance == true)
        {
            await _balanceService.RemovePendingDaysAsync(
                request.UserId,
                request.ActivityTypeId,
                request.StartDate.Year,
                request.TotalDays,
                cancellationToken);
        }

        await _unitOfWork.LeaveRequests.UpdateAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("LeaveRequest", request.Id, "RevisionRequested",
            null, new { Comment = comment }, cancellationToken);

        await _notificationService.SendNotificationAsync(request, NotificationTrigger.OnRevisionRequest, cancellationToken);

        return request;
    }

    public async Task<LeaveRequest?> GetRequestByIdAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.LeaveRequests.GetByIdAsync(requestId, cancellationToken);
    }

    public async Task<IEnumerable<LeaveRequest>> GetUserRequestsAsync(int userId, int? year = null, CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        return await _unitOfWork.LeaveRequests.FindAsync(
            r => r.UserId == userId && r.StartDate.Year == targetYear && !r.IsDeleted,
            cancellationToken);
    }

    public async Task<IEnumerable<LeaveRequest>> GetTeamRequestsAsync(int managerId, CancellationToken cancellationToken = default)
    {
        var subordinates = await _unitOfWork.UserManagers.FindAsync(
            um => um.ManagerId == managerId && um.IsActive,
            cancellationToken);

        var subordinateIds = subordinates.Select(s => s.UserId).ToList();

        return await _unitOfWork.LeaveRequests.FindAsync(
            r => subordinateIds.Contains(r.UserId) && !r.IsDeleted,
            cancellationToken);
    }

    public async Task<IEnumerable<LeaveRequest>> GetPendingApprovalsAsync(int approverId, CancellationToken cancellationToken = default)
    {
        var pendingApprovals = await _unitOfWork.RequestApprovals.FindAsync(
            a => a.ApproverId == approverId && a.Status == ApprovalStatus.Pending,
            cancellationToken);

        var requestIds = pendingApprovals.Select(a => a.RequestId).Distinct().ToList();

        return await _unitOfWork.LeaveRequests.FindAsync(
            r => requestIds.Contains(r.Id) && r.Status == RequestStatus.Pending,
            cancellationToken);
    }

    public async Task<decimal> CalculateDaysAsync(DateTime startDate, DateTime endDate, TimeTrackingMode mode, int companyId, CancellationToken cancellationToken = default)
    {
        if (mode == TimeTrackingMode.HalfDay)
        {
            return 0.5m;
        }

        var holidays = await _unitOfWork.Holidays.FindAsync(
            h => (h.CompanyId == null || h.CompanyId == companyId) &&
                 h.IsActive &&
                 h.Date >= startDate &&
                 h.Date <= endDate,
            cancellationToken);

        var holidayDates = holidays.Select(h => h.Date.Date).ToHashSet();

        decimal totalDays = 0;
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday &&
                !holidayDates.Contains(date))
            {
                totalDays += 1;
            }
        }

        return totalDays;
    }

    public async Task<bool> HasOverlappingRequestAsync(int userId, DateTime startDate, DateTime endDate, int? excludeRequestId = null, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.LeaveRequests.AnyAsync(
            r => r.UserId == userId &&
                 r.Status != RequestStatus.Cancelled &&
                 r.Status != RequestStatus.Rejected &&
                 (excludeRequestId == null || r.Id != excludeRequestId) &&
                 ((r.StartDate <= startDate && r.EndDate >= startDate) ||
                  (r.StartDate <= endDate && r.EndDate >= endDate) ||
                  (r.StartDate >= startDate && r.EndDate <= endDate)),
            cancellationToken);
    }

    private async Task<string> GenerateRequestNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _unitOfWork.LeaveRequests.CountAsync(
            r => r.CreatedAt.Year == year,
            cancellationToken);

        return $"LR-{year}-{(count + 1):D6}";
    }
}
