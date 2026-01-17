using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Interfaces;

public interface IUserService
{
    Task<UserProfile?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetUserByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserProfile>> GetUsersByCompanyAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserProfile>> GetManagersForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserProfile>> GetSubordinatesForManagerAsync(int managerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserProfile>> GetHierarchyTreeAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(int userId, PermissionType permission, int? targetCompanyId = null, CancellationToken cancellationToken = default);
    Task<bool> CanViewRequestAsync(int userId, int requestId, CancellationToken cancellationToken = default);
    Task<bool> CanApproveRequestAsync(int userId, int requestId, CancellationToken cancellationToken = default);
    Task SyncUserFromExternalAsync(string externalUserId, CancellationToken cancellationToken = default);
}
