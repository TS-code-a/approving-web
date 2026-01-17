using LeaveManagement.Core.Entities;

namespace LeaveManagement.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Company> Companies { get; }
    IRepository<CompanyRelationship> CompanyRelationships { get; }
    IRepository<UserProfile> Users { get; }
    IRepository<UserManager> UserManagers { get; }
    IRepository<UserPermission> UserPermissions { get; }
    IRepository<UserBalance> UserBalances { get; }
    IRepository<ProxyApprover> ProxyApprovers { get; }
    IRepository<ActivityType> ActivityTypes { get; }
    IRepository<ActivityField> ActivityFields { get; }
    IRepository<LeaveRequest> LeaveRequests { get; }
    IRepository<RequestApproval> RequestApprovals { get; }
    IRepository<RequestFieldValue> RequestFieldValues { get; }
    IRepository<RequestComment> RequestComments { get; }
    IRepository<NotificationTemplate> NotificationTemplates { get; }
    IRepository<Holiday> Holidays { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
