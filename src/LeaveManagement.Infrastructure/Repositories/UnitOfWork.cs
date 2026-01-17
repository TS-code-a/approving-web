using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeaveManagement.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly LeaveManagementDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<Company>? _companies;
    private IRepository<CompanyRelationship>? _companyRelationships;
    private IRepository<UserProfile>? _users;
    private IRepository<UserManager>? _userManagers;
    private IRepository<UserPermission>? _userPermissions;
    private IRepository<UserBalance>? _userBalances;
    private IRepository<ProxyApprover>? _proxyApprovers;
    private IRepository<ActivityType>? _activityTypes;
    private IRepository<ActivityField>? _activityFields;
    private IRepository<LeaveRequest>? _leaveRequests;
    private IRepository<RequestApproval>? _requestApprovals;
    private IRepository<RequestFieldValue>? _requestFieldValues;
    private IRepository<RequestComment>? _requestComments;
    private IRepository<NotificationTemplate>? _notificationTemplates;
    private IRepository<Holiday>? _holidays;

    public UnitOfWork(LeaveManagementDbContext context)
    {
        _context = context;
    }

    public IRepository<Company> Companies => _companies ??= new Repository<Company>(_context);
    public IRepository<CompanyRelationship> CompanyRelationships => _companyRelationships ??= new Repository<CompanyRelationship>(_context);
    public IRepository<UserProfile> Users => _users ??= new Repository<UserProfile>(_context);
    public IRepository<UserManager> UserManagers => _userManagers ??= new Repository<UserManager>(_context);
    public IRepository<UserPermission> UserPermissions => _userPermissions ??= new Repository<UserPermission>(_context);
    public IRepository<UserBalance> UserBalances => _userBalances ??= new Repository<UserBalance>(_context);
    public IRepository<ProxyApprover> ProxyApprovers => _proxyApprovers ??= new Repository<ProxyApprover>(_context);
    public IRepository<ActivityType> ActivityTypes => _activityTypes ??= new Repository<ActivityType>(_context);
    public IRepository<ActivityField> ActivityFields => _activityFields ??= new Repository<ActivityField>(_context);
    public IRepository<LeaveRequest> LeaveRequests => _leaveRequests ??= new Repository<LeaveRequest>(_context);
    public IRepository<RequestApproval> RequestApprovals => _requestApprovals ??= new Repository<RequestApproval>(_context);
    public IRepository<RequestFieldValue> RequestFieldValues => _requestFieldValues ??= new Repository<RequestFieldValue>(_context);
    public IRepository<RequestComment> RequestComments => _requestComments ??= new Repository<RequestComment>(_context);
    public IRepository<NotificationTemplate> NotificationTemplates => _notificationTemplates ??= new Repository<NotificationTemplate>(_context);
    public IRepository<Holiday> Holidays => _holidays ??= new Repository<Holiday>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
