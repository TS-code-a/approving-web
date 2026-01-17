namespace LeaveManagement.Core.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
}
