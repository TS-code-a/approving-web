using LeaveManagement.Shared.DTOs;

namespace LeaveManagement.Web.Services;

public class ActivityTypeService : IActivityTypeService
{
    private readonly IApiService _apiService;

    public ActivityTypeService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<ActivityTypeDto>> GetActivityTypesAsync()
    {
        var response = await _apiService.GetAsync<List<ActivityTypeDto>>("api/activitytypes");
        return response?.Data ?? new List<ActivityTypeDto>();
    }

    public async Task<ActivityTypeDto?> GetActivityTypeAsync(int id)
    {
        var response = await _apiService.GetAsync<ActivityTypeDto>($"api/activitytypes/{id}");
        return response?.Data;
    }
}
