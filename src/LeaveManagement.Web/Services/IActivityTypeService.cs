using LeaveManagement.Shared.DTOs;

namespace LeaveManagement.Web.Services;

public interface IActivityTypeService
{
    Task<List<ActivityTypeDto>> GetActivityTypesAsync();
    Task<ActivityTypeDto?> GetActivityTypeAsync(int id);
}
