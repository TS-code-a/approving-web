using LeaveManagement.Shared.Common;

namespace LeaveManagement.Web.Services;

public interface IApiService
{
    Task<ApiResponse<T>?> GetAsync<T>(string endpoint);
    Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object? data = null);
    Task<ApiResponse<T>?> PutAsync<T>(string endpoint, object data);
    Task<ApiResponse?> DeleteAsync(string endpoint);
}
