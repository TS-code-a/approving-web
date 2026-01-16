using LeaveManagement.Shared.Common;
using LeaveManagement.Shared.DTOs;

namespace LeaveManagement.Web.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly IApiService _apiService;

    public LeaveRequestService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<LeaveRequestDto>> GetMyRequestsAsync(int? year = null)
    {
        var endpoint = $"api/leaverequests?year={year ?? DateTime.UtcNow.Year}";
        var response = await _apiService.GetAsync<PagedResult<LeaveRequestDto>>(endpoint);
        return response?.Data?.Items ?? new List<LeaveRequestDto>();
    }

    public async Task<LeaveRequestDto?> GetRequestAsync(int id)
    {
        var response = await _apiService.GetAsync<LeaveRequestDto>($"api/leaverequests/{id}");
        return response?.Data;
    }

    public async Task<LeaveRequestDto?> CreateRequestAsync(LeaveRequestCreateDto dto)
    {
        var response = await _apiService.PostAsync<LeaveRequestDto>("api/leaverequests", dto);
        return response?.Data;
    }

    public async Task<LeaveRequestDto?> UpdateRequestAsync(int id, LeaveRequestUpdateDto dto)
    {
        var response = await _apiService.PutAsync<LeaveRequestDto>($"api/leaverequests/{id}", dto);
        return response?.Data;
    }

    public async Task<LeaveRequestDto?> SubmitRequestAsync(int id)
    {
        var response = await _apiService.PostAsync<LeaveRequestDto>($"api/leaverequests/{id}/submit");
        return response?.Data;
    }

    public async Task<LeaveRequestDto?> ApproveRequestAsync(int id, string? comment = null)
    {
        var response = await _apiService.PostAsync<LeaveRequestDto>($"api/leaverequests/{id}/approve", new ApprovalActionDto { Comment = comment });
        return response?.Data;
    }

    public async Task<LeaveRequestDto?> RejectRequestAsync(int id, string? comment = null)
    {
        var response = await _apiService.PostAsync<LeaveRequestDto>($"api/leaverequests/{id}/reject", new ApprovalActionDto { Comment = comment });
        return response?.Data;
    }

    public async Task<LeaveRequestDto?> CancelRequestAsync(int id, string? reason = null)
    {
        var response = await _apiService.PostAsync<LeaveRequestDto>($"api/leaverequests/{id}/cancel", new ApprovalActionDto { Comment = reason });
        return response?.Data;
    }

    public async Task<List<LeaveRequestDto>> GetPendingApprovalsAsync()
    {
        var response = await _apiService.GetAsync<List<LeaveRequestDto>>("api/leaverequests/pending-approvals");
        return response?.Data ?? new List<LeaveRequestDto>();
    }

    public async Task<List<LeaveRequestDto>> GetTeamRequestsAsync(int? year = null)
    {
        var endpoint = $"api/leaverequests/team?year={year ?? DateTime.UtcNow.Year}";
        var response = await _apiService.GetAsync<List<LeaveRequestDto>>(endpoint);
        return response?.Data ?? new List<LeaveRequestDto>();
    }

    public async Task<decimal> CalculateDaysAsync(DateTime startDate, DateTime endDate, int timeTrackingMode)
    {
        var endpoint = $"api/leaverequests/calculate-days?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&timeTrackingMode={timeTrackingMode}";
        var response = await _apiService.GetAsync<decimal>(endpoint);
        return response?.Data ?? 0;
    }
}
