using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Interfaces;

namespace LeaveManagement.Core.Services;

public class BalanceService : IBalanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public BalanceService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<UserBalance?> GetBalanceAsync(int userId, int activityTypeId, int year, CancellationToken cancellationToken = default)
    {
        return (await _unitOfWork.UserBalances.FindAsync(
            b => b.UserId == userId && b.ActivityTypeId == activityTypeId && b.Year == year,
            cancellationToken)).FirstOrDefault();
    }

    public async Task<IEnumerable<UserBalance>> GetUserBalancesAsync(int userId, int year, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.UserBalances.FindAsync(
            b => b.UserId == userId && b.Year == year,
            cancellationToken);
    }

    public async Task<UserBalance> InitializeBalanceAsync(int userId, int activityTypeId, int year, CancellationToken cancellationToken = default)
    {
        var existingBalance = await GetBalanceAsync(userId, activityTypeId, year, cancellationToken);
        if (existingBalance != null)
        {
            return existingBalance;
        }

        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(activityTypeId, cancellationToken)
            ?? throw new InvalidOperationException("Activity type not found");

        var balance = new UserBalance
        {
            UserId = userId,
            ActivityTypeId = activityTypeId,
            Year = year,
            TotalDays = activityType.DefaultAnnualBalance ?? 0,
            UsedDays = 0,
            PendingDays = 0,
            CarriedOverDays = 0,
            AdjustmentDays = 0,
            CreatedBy = _currentUserService.UserName
        };

        var createdBalance = await _unitOfWork.UserBalances.AddAsync(balance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UserBalance", createdBalance.Id, "Initialized",
            null, balance, cancellationToken);

        return createdBalance;
    }

    public async Task DeductBalanceAsync(int userId, int activityTypeId, int year, decimal days, CancellationToken cancellationToken = default)
    {
        var balance = await GetBalanceAsync(userId, activityTypeId, year, cancellationToken)
            ?? await InitializeBalanceAsync(userId, activityTypeId, year, cancellationToken);

        var oldUsedDays = balance.UsedDays;
        balance.UsedDays += days;
        balance.UpdatedAt = DateTime.UtcNow;
        balance.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.UserBalances.UpdateAsync(balance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UserBalance", balance.Id, "Deducted",
            new { UsedDays = oldUsedDays }, new { balance.UsedDays, DeductedDays = days }, cancellationToken);
    }

    public async Task RestoreBalanceAsync(int userId, int activityTypeId, int year, decimal days, CancellationToken cancellationToken = default)
    {
        var balance = await GetBalanceAsync(userId, activityTypeId, year, cancellationToken);
        if (balance == null) return;

        var oldUsedDays = balance.UsedDays;
        balance.UsedDays = Math.Max(0, balance.UsedDays - days);
        balance.UpdatedAt = DateTime.UtcNow;
        balance.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.UserBalances.UpdateAsync(balance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UserBalance", balance.Id, "Restored",
            new { UsedDays = oldUsedDays }, new { balance.UsedDays, RestoredDays = days }, cancellationToken);
    }

    public async Task AddPendingDaysAsync(int userId, int activityTypeId, int year, decimal days, CancellationToken cancellationToken = default)
    {
        var balance = await GetBalanceAsync(userId, activityTypeId, year, cancellationToken)
            ?? await InitializeBalanceAsync(userId, activityTypeId, year, cancellationToken);

        var oldPendingDays = balance.PendingDays;
        balance.PendingDays += days;
        balance.UpdatedAt = DateTime.UtcNow;
        balance.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.UserBalances.UpdateAsync(balance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UserBalance", balance.Id, "PendingAdded",
            new { PendingDays = oldPendingDays }, new { balance.PendingDays }, cancellationToken);
    }

    public async Task RemovePendingDaysAsync(int userId, int activityTypeId, int year, decimal days, CancellationToken cancellationToken = default)
    {
        var balance = await GetBalanceAsync(userId, activityTypeId, year, cancellationToken);
        if (balance == null) return;

        var oldPendingDays = balance.PendingDays;
        balance.PendingDays = Math.Max(0, balance.PendingDays - days);
        balance.UpdatedAt = DateTime.UtcNow;
        balance.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.UserBalances.UpdateAsync(balance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UserBalance", balance.Id, "PendingRemoved",
            new { PendingDays = oldPendingDays }, new { balance.PendingDays }, cancellationToken);
    }

    public async Task AdjustBalanceAsync(int userId, int activityTypeId, int year, decimal adjustmentDays, string? reason = null, CancellationToken cancellationToken = default)
    {
        var balance = await GetBalanceAsync(userId, activityTypeId, year, cancellationToken)
            ?? await InitializeBalanceAsync(userId, activityTypeId, year, cancellationToken);

        var oldAdjustment = balance.AdjustmentDays;
        balance.AdjustmentDays += adjustmentDays;
        balance.UpdatedAt = DateTime.UtcNow;
        balance.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.UserBalances.UpdateAsync(balance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("UserBalance", balance.Id, "Adjusted",
            new { AdjustmentDays = oldAdjustment },
            new { balance.AdjustmentDays, AdjustedBy = adjustmentDays, Reason = reason },
            cancellationToken);
    }

    public async Task ProcessCarryOverAsync(int userId, int fromYear, CancellationToken cancellationToken = default)
    {
        var previousBalances = await GetUserBalancesAsync(userId, fromYear, cancellationToken);
        var toYear = fromYear + 1;

        foreach (var prevBalance in previousBalances)
        {
            var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(prevBalance.ActivityTypeId, cancellationToken);

            if (activityType == null || !activityType.AllowCarryOver)
            {
                continue;
            }

            var carryOverDays = prevBalance.AvailableDays;

            if (activityType.MaxCarryOverDays.HasValue)
            {
                carryOverDays = Math.Min(carryOverDays, activityType.MaxCarryOverDays.Value);
            }

            if (carryOverDays <= 0)
            {
                continue;
            }

            var newBalance = await GetBalanceAsync(userId, prevBalance.ActivityTypeId, toYear, cancellationToken)
                ?? await InitializeBalanceAsync(userId, prevBalance.ActivityTypeId, toYear, cancellationToken);

            newBalance.CarriedOverDays = carryOverDays;
            newBalance.UpdatedAt = DateTime.UtcNow;
            newBalance.UpdatedBy = _currentUserService.UserName;

            await _unitOfWork.UserBalances.UpdateAsync(newBalance, cancellationToken);

            await _auditService.LogAsync("UserBalance", newBalance.Id, "CarryOver",
                new { FromYear = fromYear },
                new { CarriedOverDays = carryOverDays },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasSufficientBalanceAsync(int userId, int activityTypeId, int year, decimal requiredDays, CancellationToken cancellationToken = default)
    {
        var balance = await GetBalanceAsync(userId, activityTypeId, year, cancellationToken);

        if (balance == null)
        {
            var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(activityTypeId, cancellationToken);
            return (activityType?.DefaultAnnualBalance ?? 0) >= requiredDays;
        }

        return balance.AvailableDays >= requiredDays;
    }
}
