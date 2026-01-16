using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Interfaces;

public interface ILeaveRequestService
{
    Task<LeaveRequest> CreateRequestAsync(LeaveRequest request, CancellationToken cancellationToken = default);
    Task<LeaveRequest> SubmitRequestAsync(int requestId, int userId, CancellationToken cancellationToken = default);
    Task<LeaveRequest> ApproveRequestAsync(int requestId, int approverId, string? comment = null, CancellationToken cancellationToken = default);
    Task<LeaveRequest> RejectRequestAsync(int requestId, int approverId, string? comment = null, CancellationToken cancellationToken = default);
    Task<LeaveRequest> CancelRequestAsync(int requestId, int userId, string? reason = null, CancellationToken cancellationToken = default);
    Task<LeaveRequest> RequestRevisionAsync(int requestId, int approverId, string comment, CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetRequestByIdAsync(int requestId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequest>> GetUserRequestsAsync(int userId, int? year = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequest>> GetTeamRequestsAsync(int managerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequest>> GetPendingApprovalsAsync(int approverId, CancellationToken cancellationToken = default);
    Task<decimal> CalculateDaysAsync(DateTime startDate, DateTime endDate, TimeTrackingMode mode, int companyId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingRequestAsync(int userId, DateTime startDate, DateTime endDate, int? excludeRequestId = null, CancellationToken cancellationToken = default);
}
