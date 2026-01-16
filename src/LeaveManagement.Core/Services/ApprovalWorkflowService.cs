using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Interfaces;

namespace LeaveManagement.Core.Services;

public class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;

    public ApprovalWorkflowService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RequestApproval>> GenerateApprovalChainAsync(LeaveRequest request, CancellationToken cancellationToken = default)
    {
        var activityType = await _unitOfWork.ActivityTypes.GetByIdAsync(request.ActivityTypeId, cancellationToken)
            ?? throw new InvalidOperationException("Activity type not found");

        if (activityType.ApprovalWorkflow == ApprovalWorkflowType.AutoApprove || !activityType.RequiresApproval)
        {
            return Enumerable.Empty<RequestApproval>();
        }

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        var approvers = await GetApproversForUserAsync(request.UserId, request.ActivityTypeId, cancellationToken);
        var approverList = approvers.ToList();

        if (!approverList.Any())
        {
            throw new InvalidOperationException("No approvers found for this request");
        }

        var approvals = new List<RequestApproval>();
        var sequence = 1;

        switch (activityType.ApprovalWorkflow)
        {
            case ApprovalWorkflowType.SingleLevel:
                foreach (var approver in approverList.Where(a => GetApproverLevel(a, user.Id).GetAwaiter().GetResult() == 1))
                {
                    approvals.Add(CreateApproval(request.Id, approver.Id, 1, sequence++, user.ApprovalLogic == ApprovalLogicType.AllManagers));
                }
                if (!approvals.Any() && approverList.Any())
                {
                    var firstApprover = approverList.First();
                    approvals.Add(CreateApproval(request.Id, firstApprover.Id, 1, 1, true));
                }
                break;

            case ApprovalWorkflowType.MultiLevel:
                var groupedByLevel = new Dictionary<int, List<UserProfile>>();
                foreach (var approver in approverList)
                {
                    var level = await GetApproverLevel(approver, user.Id);
                    if (!groupedByLevel.ContainsKey(level))
                    {
                        groupedByLevel[level] = new List<UserProfile>();
                    }
                    groupedByLevel[level].Add(approver);
                }

                var maxLevels = activityType.MaxApprovalLevels ?? int.MaxValue;
                var processedLevels = 0;

                foreach (var level in groupedByLevel.Keys.OrderBy(k => k))
                {
                    if (processedLevels >= maxLevels) break;

                    foreach (var approver in groupedByLevel[level])
                    {
                        approvals.Add(CreateApproval(request.Id, approver.Id, level, sequence++,
                            user.ApprovalLogic == ApprovalLogicType.AllManagers));
                    }
                    processedLevels++;
                }
                break;

            case ApprovalWorkflowType.SkipLevel:
                var skipLevelApprovers = new List<UserProfile>();
                foreach (var approver in approverList)
                {
                    var level = await GetApproverLevel(approver, user.Id);
                    if (level > 1)
                    {
                        skipLevelApprovers.Add(approver);
                    }
                }

                if (!skipLevelApprovers.Any())
                {
                    skipLevelApprovers = approverList;
                }

                foreach (var approver in skipLevelApprovers)
                {
                    approvals.Add(CreateApproval(request.Id, approver.Id, 1, sequence++,
                        user.ApprovalLogic == ApprovalLogicType.AllManagers));
                }
                break;
        }

        return approvals;
    }

    public async Task<IEnumerable<UserProfile>> GetApproversForUserAsync(int userId, int activityTypeId, CancellationToken cancellationToken = default)
    {
        var managerRelationships = await _unitOfWork.UserManagers.FindAsync(
            um => um.UserId == userId && um.IsActive,
            cancellationToken);

        var managerIds = managerRelationships.Select(um => um.ManagerId).ToList();

        var managers = await _unitOfWork.Users.FindAsync(
            u => managerIds.Contains(u.Id) && u.IsActive,
            cancellationToken);

        var result = new List<UserProfile>();
        foreach (var manager in managers)
        {
            var proxyApprover = await GetProxyApproverAsync(manager.Id, DateTime.UtcNow, cancellationToken);
            result.Add(proxyApprover ?? manager);
        }

        return result;
    }

    public async Task<bool> IsApprovalCompleteAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found");

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        var approvals = await _unitOfWork.RequestApprovals.FindAsync(
            a => a.RequestId == requestId,
            cancellationToken);

        var approvalList = approvals.ToList();

        if (!approvalList.Any())
        {
            return true;
        }

        if (approvalList.Any(a => a.Status == ApprovalStatus.Rejected))
        {
            return false;
        }

        if (user.ApprovalLogic == ApprovalLogicType.AnyManager)
        {
            var levels = approvalList.Select(a => a.Level).Distinct().OrderBy(l => l).ToList();

            foreach (var level in levels)
            {
                var levelApprovals = approvalList.Where(a => a.Level == level).ToList();
                var anyApproved = levelApprovals.Any(a => a.Status == ApprovalStatus.Approved);

                if (!anyApproved)
                {
                    return false;
                }
            }

            return true;
        }
        else
        {
            return approvalList.All(a => a.Status == ApprovalStatus.Approved || a.Status == ApprovalStatus.Skipped);
        }
    }

    public async Task<RequestApproval?> GetNextPendingApprovalAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var approvals = await _unitOfWork.RequestApprovals.FindAsync(
            a => a.RequestId == requestId && a.Status == ApprovalStatus.Pending,
            cancellationToken);

        return approvals.OrderBy(a => a.Level).ThenBy(a => a.Sequence).FirstOrDefault();
    }

    public async Task<UserProfile?> GetProxyApproverAsync(int approverId, DateTime date, CancellationToken cancellationToken = default)
    {
        var proxyAssignment = (await _unitOfWork.ProxyApprovers.FindAsync(
            p => p.OriginalApproverId == approverId &&
                 p.IsActive &&
                 p.StartDate <= date &&
                 p.EndDate >= date,
            cancellationToken)).FirstOrDefault();

        if (proxyAssignment == null)
        {
            return null;
        }

        return await _unitOfWork.Users.GetByIdAsync(proxyAssignment.ProxyUserId, cancellationToken);
    }

    private RequestApproval CreateApproval(int requestId, int approverId, int level, int sequence, bool isRequired)
    {
        return new RequestApproval
        {
            RequestId = requestId,
            ApproverId = approverId,
            Level = level,
            Sequence = sequence,
            Status = ApprovalStatus.Pending,
            IsRequired = isRequired
        };
    }

    private async Task<int> GetApproverLevel(UserProfile approver, int userId)
    {
        var relationship = (await _unitOfWork.UserManagers.FindAsync(
            um => um.UserId == userId && um.ManagerId == approver.Id && um.IsActive)).FirstOrDefault();

        return relationship?.Level ?? 1;
    }
}
