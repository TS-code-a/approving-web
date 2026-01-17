using LeaveManagement.Core.Entities;

namespace LeaveManagement.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(string entityType, int entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string entityType, int entityId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(int userId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
