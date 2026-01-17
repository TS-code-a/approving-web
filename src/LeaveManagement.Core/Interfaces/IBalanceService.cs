using LeaveManagement.Core.Entities;

namespace LeaveManagement.Core.Interfaces;

public interface IBalanceService
{
    Task<UserBalance?> GetBalanceAsync(int userId, int activityTypeId, int year, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserBalance>> GetUserBalancesAsync(int userId, int year, CancellationToken cancellationToken = default);
    Task<UserBalance> InitializeBalanceAsync(int userId, int activityTypeId, int year, CancellationToken cancellationToken = default);
    Task DeductBalanceAsync(int userId, int activityTypeId, int year, decimal days, CancellationToken cancellationToken = default);
    Task RestoreBalanceAsync(int userId, int activityTypeId, int year, decimal days, CancellationToken cancellationToken = default);
    Task AddPendingDaysAsync(int userId, int activityTypeId, int year, decimal days, CancellationToken cancellationToken = default);
    Task RemovePendingDaysAsync(int userId, int activityTypeId, int year, decimal days, CancellationToken cancellationToken = default);
    Task AdjustBalanceAsync(int userId, int activityTypeId, int year, decimal adjustmentDays, string? reason = null, CancellationToken cancellationToken = default);
    Task ProcessCarryOverAsync(int userId, int fromYear, CancellationToken cancellationToken = default);
    Task<bool> HasSufficientBalanceAsync(int userId, int activityTypeId, int year, decimal requiredDays, CancellationToken cancellationToken = default);
}
