using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Interfaces;

namespace LeaveManagement.Core.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfile?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<UserProfile?> GetUserByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.ExternalUserId == externalUserId && u.IsActive,
            cancellationToken);
    }

    public async Task<UserProfile?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email.ToLower() && u.IsActive,
            cancellationToken);
    }

    public async Task<IEnumerable<UserProfile>> GetUsersByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Users.FindAsync(
            u => u.CompanyId == companyId && u.IsActive,
            cancellationToken);
    }

    public async Task<IEnumerable<UserProfile>> GetManagersForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var managerRelationships = await _unitOfWork.UserManagers.FindAsync(
            um => um.UserId == userId && um.IsActive,
            cancellationToken);

        var managerIds = managerRelationships.Select(um => um.ManagerId).ToList();

        return await _unitOfWork.Users.FindAsync(
            u => managerIds.Contains(u.Id) && u.IsActive,
            cancellationToken);
    }

    public async Task<IEnumerable<UserProfile>> GetSubordinatesForManagerAsync(int managerId, CancellationToken cancellationToken = default)
    {
        var subordinateRelationships = await _unitOfWork.UserManagers.FindAsync(
            um => um.ManagerId == managerId && um.IsActive,
            cancellationToken);

        var subordinateIds = subordinateRelationships.Select(um => um.UserId).ToList();

        return await _unitOfWork.Users.FindAsync(
            u => subordinateIds.Contains(u.Id) && u.IsActive,
            cancellationToken);
    }

    public async Task<IEnumerable<UserProfile>> GetHierarchyTreeAsync(int userId, CancellationToken cancellationToken = default)
    {
        var result = new List<UserProfile>();
        var visited = new HashSet<int>();

        await GetSubordinatesRecursiveAsync(userId, result, visited, cancellationToken);

        return result;
    }

    private async Task GetSubordinatesRecursiveAsync(int managerId, List<UserProfile> result, HashSet<int> visited, CancellationToken cancellationToken)
    {
        if (visited.Contains(managerId))
        {
            return;
        }

        visited.Add(managerId);

        var subordinates = await GetSubordinatesForManagerAsync(managerId, cancellationToken);

        foreach (var subordinate in subordinates)
        {
            if (!visited.Contains(subordinate.Id))
            {
                result.Add(subordinate);
                await GetSubordinatesRecursiveAsync(subordinate.Id, result, visited, cancellationToken);
            }
        }
    }

    public async Task<bool> HasPermissionAsync(int userId, PermissionType permission, int? targetCompanyId = null, CancellationToken cancellationToken = default)
    {
        var permissions = await _unitOfWork.UserPermissions.FindAsync(
            p => p.UserId == userId &&
                 p.IsActive &&
                 (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow),
            cancellationToken);

        foreach (var p in permissions)
        {
            if (p.PermissionType == PermissionType.SystemAdmin)
            {
                return true;
            }

            if (p.PermissionType == permission)
            {
                if (targetCompanyId == null || p.TargetCompanyId == null || p.TargetCompanyId == targetCompanyId)
                {
                    return true;
                }
            }

            if (permission == PermissionType.CrossCompanyViewer && p.PermissionType == PermissionType.CrossCompanyViewer)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> CanViewRequestAsync(int userId, int requestId, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            return false;
        }

        if (request.UserId == userId)
        {
            return true;
        }

        if (await HasPermissionAsync(userId, PermissionType.SystemAdmin, null, cancellationToken))
        {
            return true;
        }

        var requestUser = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (requestUser == null)
        {
            return false;
        }

        if (await HasPermissionAsync(userId, PermissionType.HRViewer, requestUser.CompanyId, cancellationToken))
        {
            return true;
        }

        if (await HasPermissionAsync(userId, PermissionType.CrossCompanyViewer, null, cancellationToken))
        {
            return true;
        }

        var subordinates = await GetHierarchyTreeAsync(userId, cancellationToken);
        if (subordinates.Any(s => s.Id == request.UserId))
        {
            return true;
        }

        var isApprover = await _unitOfWork.RequestApprovals.AnyAsync(
            a => a.RequestId == requestId && a.ApproverId == userId,
            cancellationToken);

        return isApprover;
    }

    public async Task<bool> CanApproveRequestAsync(int userId, int requestId, CancellationToken cancellationToken = default)
    {
        var pendingApproval = await _unitOfWork.RequestApprovals.FirstOrDefaultAsync(
            a => a.RequestId == requestId &&
                 a.ApproverId == userId &&
                 a.Status == ApprovalStatus.Pending,
            cancellationToken);

        if (pendingApproval != null)
        {
            return true;
        }

        var originalApprovals = await _unitOfWork.RequestApprovals.FindAsync(
            a => a.RequestId == requestId && a.Status == ApprovalStatus.Pending,
            cancellationToken);

        foreach (var approval in originalApprovals)
        {
            var proxyAssignment = await _unitOfWork.ProxyApprovers.FirstOrDefaultAsync(
                p => p.OriginalApproverId == approval.ApproverId &&
                     p.ProxyUserId == userId &&
                     p.IsActive &&
                     p.StartDate <= DateTime.UtcNow &&
                     p.EndDate >= DateTime.UtcNow,
                cancellationToken);

            if (proxyAssignment != null)
            {
                return true;
            }
        }

        return false;
    }

    public Task SyncUserFromExternalAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        // This would connect to external database/ERP system
        // Implementation depends on the external system integration
        throw new NotImplementedException("External user sync needs to be implemented based on the specific external system");
    }
}
