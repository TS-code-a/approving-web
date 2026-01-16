using LeaveManagement.Shared.DTOs;

namespace LeaveManagement.Web.Services;

public class UserService : IUserService
{
    private readonly IApiService _apiService;

    public UserService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var response = await _apiService.GetAsync<UserDto>("api/users/me");
        return response?.Data;
    }

    public async Task<List<UserBalanceDto>> GetMyBalancesAsync(int? year = null)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return new List<UserBalanceDto>();

        var endpoint = $"api/users/{user.Id}/balances?year={year ?? DateTime.UtcNow.Year}";
        var response = await _apiService.GetAsync<List<UserBalanceDto>>(endpoint);
        return response?.Data ?? new List<UserBalanceDto>();
    }

    public async Task<DashboardDto?> GetDashboardAsync()
    {
        var response = await _apiService.GetAsync<DashboardDto>("api/dashboard");
        return response?.Data;
    }
}
