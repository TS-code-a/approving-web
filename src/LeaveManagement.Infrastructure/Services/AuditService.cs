using System.Text.Json;
using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly LeaveManagementDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AuditService(LeaveManagementDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task LogAsync(string entityType, int entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            UserId = _currentUserService.UserId,
            UserName = _currentUserService.UserName,
            IpAddress = _currentUserService.IpAddress,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string entityType, int entityId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(int userId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.Where(a => a.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(a => a.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.Timestamp <= to.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
