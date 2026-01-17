using LeaveManagement.Shared.DTOs;

namespace LeaveManagement.Web.Services;

public interface IUserService
{
    Task<UserDto?> GetCurrentUserAsync();
    Task<List<UserBalanceDto>> GetMyBalancesAsync(int? year = null);
    Task<DashboardDto?> GetDashboardAsync();
}
