using LeaveManagement.Core.Entities;

namespace LeaveManagement.Core.Interfaces;

public interface IApprovalWorkflowService
{
    Task<IEnumerable<RequestApproval>> GenerateApprovalChainAsync(LeaveRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserProfile>> GetApproversForUserAsync(int userId, int activityTypeId, CancellationToken cancellationToken = default);
    Task<bool> IsApprovalCompleteAsync(int requestId, CancellationToken cancellationToken = default);
    Task<RequestApproval?> GetNextPendingApprovalAsync(int requestId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetProxyApproverAsync(int approverId, DateTime date, CancellationToken cancellationToken = default);
}
