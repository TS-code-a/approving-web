using LeaveManagement.Shared.DTOs;

namespace LeaveManagement.Web.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<string?> GetTokenAsync();
}
