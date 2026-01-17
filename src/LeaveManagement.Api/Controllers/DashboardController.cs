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
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly ILeaveRequestService _requestService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(
        IUnitOfWork unitOfWork,
        IUserService userService,
        ILeaveRequestService requestService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _requestService = requestService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var currentYear = DateTime.UtcNow.Year;
        var today = DateTime.UtcNow.Date;

        // Get pending approvals count
        var pendingApprovals = await _requestService.GetPendingApprovalsAsync(userId);
        var pendingApprovalsList = pendingApprovals.ToList();

        // Get user's pending requests
        var myRequests = await _unitOfWork.LeaveRequests
            .Query()
            .Include(r => r.ActivityType)
            .Where(r => r.UserId == userId && r.Status == RequestStatus.Pending)
            .ToListAsync();

        // Get user's balances
        var balances = await _unitOfWork.UserBalances
            .Query()
            .Include(b => b.ActivityType)
            .Where(b => b.UserId == userId && b.Year == currentYear)
            .ToListAsync();

        var balanceDtos = balances.Select(b => new UserBalanceDto
        {
            Id = b.Id,
            UserId = b.UserId,
            ActivityTypeId = b.ActivityTypeId,
            ActivityTypeName = b.ActivityType?.Name ?? string.Empty,
            ActivityTypeColor = b.ActivityType?.Color,
            Year = b.Year,
            TotalDays = b.TotalDays,
            UsedDays = b.UsedDays,
            PendingDays = b.PendingDays,
            CarriedOverDays = b.CarriedOverDays,
            AdjustmentDays = b.AdjustmentDays
        }).ToList();

        // Get recent requests
        var recentRequests = await _unitOfWork.LeaveRequests
            .Query()
            .Include(r => r.User)
            .Include(r => r.ActivityType)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentDtos = recentRequests.Select(MapToLeaveRequestDto).ToList();

        // Get pending approvals details
        var pendingApprovalDtos = new List<LeaveRequestDto>();
        foreach (var request in pendingApprovalsList.Take(5))
        {
            var fullRequest = await _unitOfWork.LeaveRequests
                .Query()
                .Include(r => r.User)
                .Include(r => r.ActivityType)
                .FirstOrDefaultAsync(r => r.Id == request.Id);

            if (fullRequest != null)
            {
                pendingApprovalDtos.Add(MapToLeaveRequestDto(fullRequest));
            }
        }

        // Get upcoming events
        var upcomingRequests = await _unitOfWork.LeaveRequests
            .Query()
            .Include(r => r.User)
            .Include(r => r.ActivityType)
            .Where(r => r.UserId == userId &&
                       r.Status == RequestStatus.Approved &&
                       r.StartDate >= today &&
                       r.StartDate <= today.AddDays(30))
            .OrderBy(r => r.StartDate)
            .Take(5)
            .ToListAsync();

        var upcomingEvents = upcomingRequests.Select(r => new CalendarEventDto
        {
            Id = r.Id,
            Title = r.ActivityType?.Name ?? "Leave",
            Start = r.StartDate,
            End = r.EndDate,
            Color = r.ActivityType?.Color,
            EventType = "LeaveRequest",
            RequestId = r.Id,
            ActivityTypeName = r.ActivityType?.Name
        }).ToList();

        // Check if user is a manager for additional stats
        var subordinates = await _userService.GetSubordinatesForManagerAsync(userId);
        var subordinateList = subordinates.ToList();

        if (subordinateList.Any())
        {
            // Get team members on leave today
            var subordinateIds = subordinateList.Select(s => s.Id).ToList();
            var teamOnLeaveToday = await _unitOfWork.LeaveRequests.CountAsync(
                r => subordinateIds.Contains(r.UserId) &&
                     r.Status == RequestStatus.Approved &&
                     r.StartDate <= today &&
                     r.EndDate >= today);

            return Ok(ApiResponse<DashboardDto>.Ok(new ManagerDashboardDto
            {
                PendingApprovalsCount = pendingApprovalsList.Count,
                MyPendingRequestsCount = myRequests.Count,
                TeamOnLeaveToday = teamOnLeaveToday,
                MyBalances = balanceDtos,
                RecentRequests = recentDtos,
                PendingApprovals = pendingApprovalDtos,
                UpcomingEvents = upcomingEvents,
                TotalTeamMembers = subordinateList.Count,
                TeamPendingRequestsCount = await _unitOfWork.LeaveRequests.CountAsync(
                    r => subordinateIds.Contains(r.UserId) && r.Status == RequestStatus.Pending),
                TeamStatus = await GetTeamStatus(subordinateIds, today)
            }));
        }

        return Ok(ApiResponse<DashboardDto>.Ok(new DashboardDto
        {
            PendingApprovalsCount = pendingApprovalsList.Count,
            MyPendingRequestsCount = myRequests.Count,
            TeamOnLeaveToday = 0,
            MyBalances = balanceDtos,
            RecentRequests = recentDtos,
            PendingApprovals = pendingApprovalDtos,
            UpcomingEvents = upcomingEvents
        }));
    }

    [HttpGet("admin")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetAdminDashboard()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var pendingApprovals = await _requestService.GetPendingApprovalsAsync(userId);
        var pendingApprovalsList = pendingApprovals.ToList();

        // Get company-wide stats
        var totalCompanies = await _unitOfWork.Companies.CountAsync(c => c.IsActive);
        var totalUsers = await _unitOfWork.Users.CountAsync(u => u.IsActive);

        var monthlyRequests = await _unitOfWork.LeaveRequests
            .Query()
            .Where(r => r.CreatedAt >= monthStart && r.CreatedAt <= monthEnd)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalRequestsThisMonth = monthlyRequests.Sum(r => r.Count);
        var approvedRequestsThisMonth = monthlyRequests.FirstOrDefault(r => r.Status == RequestStatus.Approved)?.Count ?? 0;
        var rejectedRequestsThisMonth = monthlyRequests.FirstOrDefault(r => r.Status == RequestStatus.Rejected)?.Count ?? 0;

        // Activity type summary
        var activitySummary = await _unitOfWork.LeaveRequests
            .Query()
            .Include(r => r.ActivityType)
            .Where(r => r.CreatedAt >= monthStart && r.CreatedAt <= monthEnd)
            .GroupBy(r => new { r.ActivityTypeId, r.ActivityType!.Name, r.ActivityType.Color })
            .Select(g => new ActivityTypeSummaryDto
            {
                ActivityTypeId = g.Key.ActivityTypeId,
                ActivityTypeName = g.Key.Name,
                Color = g.Key.Color,
                TotalRequests = g.Count(),
                ApprovedRequests = g.Count(r => r.Status == RequestStatus.Approved),
                PendingRequests = g.Count(r => r.Status == RequestStatus.Pending),
                RejectedRequests = g.Count(r => r.Status == RequestStatus.Rejected),
                TotalDaysUsed = g.Where(r => r.Status == RequestStatus.Approved).Sum(r => r.TotalDays)
            })
            .ToListAsync();

        // Team on leave today (all users for admin)
        var teamOnLeaveToday = await _unitOfWork.LeaveRequests.CountAsync(
            r => r.Status == RequestStatus.Approved &&
                 r.StartDate <= today &&
                 r.EndDate >= today);

        return Ok(ApiResponse<AdminDashboardDto>.Ok(new AdminDashboardDto
        {
            PendingApprovalsCount = pendingApprovalsList.Count,
            MyPendingRequestsCount = 0,
            TeamOnLeaveToday = teamOnLeaveToday,
            MyBalances = new List<UserBalanceDto>(),
            RecentRequests = new List<LeaveRequestDto>(),
            PendingApprovals = new List<LeaveRequestDto>(),
            UpcomingEvents = new List<CalendarEventDto>(),
            TotalTeamMembers = totalUsers,
            TeamPendingRequestsCount = await _unitOfWork.LeaveRequests.CountAsync(r => r.Status == RequestStatus.Pending),
            TeamStatus = new List<TeamMemberStatusDto>(),
            TotalCompanies = totalCompanies,
            TotalUsers = totalUsers,
            TotalRequestsThisMonth = totalRequestsThisMonth,
            ApprovedRequestsThisMonth = approvedRequestsThisMonth,
            RejectedRequestsThisMonth = rejectedRequestsThisMonth,
            ActivityTypeSummary = activitySummary
        }));
    }

    private async Task<List<TeamMemberStatusDto>> GetTeamStatus(List<int> teamMemberIds, DateTime today)
    {
        var result = new List<TeamMemberStatusDto>();

        foreach (var memberId in teamMemberIds.Take(10))
        {
            var member = await _unitOfWork.Users.GetByIdAsync(memberId);
            if (member == null) continue;

            var currentLeave = await _unitOfWork.LeaveRequests
                .Query()
                .Include(r => r.ActivityType)
                .FirstOrDefaultAsync(r =>
                    r.UserId == memberId &&
                    r.Status == RequestStatus.Approved &&
                    r.StartDate <= today &&
                    r.EndDate >= today);

            var pendingCount = await _unitOfWork.LeaveRequests.CountAsync(
                r => r.UserId == memberId && r.Status == RequestStatus.Pending);

            result.Add(new TeamMemberStatusDto
            {
                UserId = memberId,
                UserName = member.FullName,
                Department = member.Department,
                IsOnLeave = currentLeave != null,
                CurrentLeaveType = currentLeave?.ActivityType?.Name,
                ReturnDate = currentLeave?.EndDate.AddDays(1),
                PendingRequestsCount = pendingCount
            });
        }

        return result;
    }

    private static LeaveRequestDto MapToLeaveRequestDto(Core.Entities.LeaveRequest request)
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
            TotalDays = request.TotalDays,
            Reason = request.Reason,
            CreatedAt = request.CreatedAt
        };
    }
}
