using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Shared.DTOs;

public class HolidayDto
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsRecurringYearly { get; set; }
    public bool IsHalfDay { get; set; }
    public bool IsActive { get; set; }
}

public class HolidayCreateDto
{
    public int? CompanyId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    public bool IsRecurringYearly { get; set; }
    public bool IsHalfDay { get; set; }
}

public class HolidayUpdateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    public bool IsRecurringYearly { get; set; }
    public bool IsHalfDay { get; set; }
    public bool IsActive { get; set; }
}
