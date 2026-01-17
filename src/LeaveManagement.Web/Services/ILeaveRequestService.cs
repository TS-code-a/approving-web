using LeaveManagement.Shared.Common;
using LeaveManagement.Shared.DTOs;

namespace LeaveManagement.Web.Services;

public interface ILeaveRequestService
{
    Task<List<LeaveRequestDto>> GetMyRequestsAsync(int? year = null);
    Task<LeaveRequestDto?> GetRequestAsync(int id);
    Task<LeaveRequestDto?> CreateRequestAsync(LeaveRequestCreateDto dto);
    Task<LeaveRequestDto?> UpdateRequestAsync(int id, LeaveRequestUpdateDto dto);
    Task<LeaveRequestDto?> SubmitRequestAsync(int id);
    Task<LeaveRequestDto?> ApproveRequestAsync(int id, string? comment = null);
    Task<LeaveRequestDto?> RejectRequestAsync(int id, string? comment = null);
    Task<LeaveRequestDto?> CancelRequestAsync(int id, string? reason = null);
    Task<List<LeaveRequestDto>> GetPendingApprovalsAsync();
    Task<List<LeaveRequestDto>> GetTeamRequestsAsync(int? year = null);
    Task<decimal> CalculateDaysAsync(DateTime startDate, DateTime endDate, int timeTrackingMode);
}
