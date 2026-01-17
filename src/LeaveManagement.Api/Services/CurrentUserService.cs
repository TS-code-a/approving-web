using System.Security.Claims;
using LeaveManagement.Core.Interfaces;

namespace LeaveManagement.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;

            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;

    public string? UserName => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value
        ?? _httpContextAccessor.HttpContext?.User.Identity?.Name;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
