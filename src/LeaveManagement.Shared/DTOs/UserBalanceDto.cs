namespace LeaveManagement.Shared.DTOs;

public class UserBalanceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ActivityTypeId { get; set; }
    public string ActivityTypeName { get; set; } = string.Empty;
    public string? ActivityTypeColor { get; set; }
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal CarriedOverDays { get; set; }
    public decimal AdjustmentDays { get; set; }
    public decimal AvailableDays => TotalDays + CarriedOverDays + AdjustmentDays - UsedDays - PendingDays;
}

public class BalanceAdjustmentDto
{
    public int UserId { get; set; }
    public int ActivityTypeId { get; set; }
    public int Year { get; set; }
    public decimal AdjustmentDays { get; set; }
    public string? Reason { get; set; }
}

public class BalanceSummaryDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Year { get; set; }
    public List<UserBalanceDto> Balances { get; set; } = new();
}
