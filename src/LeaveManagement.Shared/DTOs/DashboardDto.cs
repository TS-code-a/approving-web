namespace LeaveManagement.Shared.DTOs;

public class DashboardDto
{
    public int PendingApprovalsCount { get; set; }
    public int MyPendingRequestsCount { get; set; }
    public int TeamOnLeaveToday { get; set; }
    public List<UserBalanceDto> MyBalances { get; set; } = new();
    public List<LeaveRequestDto> RecentRequests { get; set; } = new();
    public List<LeaveRequestDto> PendingApprovals { get; set; } = new();
    public List<CalendarEventDto> UpcomingEvents { get; set; } = new();
}

public class ManagerDashboardDto : DashboardDto
{
    public int TotalTeamMembers { get; set; }
    public int TeamPendingRequestsCount { get; set; }
    public List<TeamMemberStatusDto> TeamStatus { get; set; } = new();
}

public class TeamMemberStatusDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool IsOnLeave { get; set; }
    public string? CurrentLeaveType { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int PendingRequestsCount { get; set; }
}

public class AdminDashboardDto : ManagerDashboardDto
{
    public int TotalCompanies { get; set; }
    public int TotalUsers { get; set; }
    public int TotalRequestsThisMonth { get; set; }
    public int ApprovedRequestsThisMonth { get; set; }
    public int RejectedRequestsThisMonth { get; set; }
    public List<ActivityTypeSummaryDto> ActivityTypeSummary { get; set; } = new();
}

public class ActivityTypeSummaryDto
{
    public int ActivityTypeId { get; set; }
    public string ActivityTypeName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int TotalRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int PendingRequests { get; set; }
    public int RejectedRequests { get; set; }
    public decimal TotalDaysUsed { get; set; }
}
