namespace LeaveManagement.Shared.DTOs;

public class CalendarEventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Color { get; set; }
    public bool AllDay { get; set; } = true;
    public string EventType { get; set; } = string.Empty;

    // Leave Request specific
    public int? RequestId { get; set; }
    public string? UserName { get; set; }
    public string? ActivityTypeName { get; set; }
    public string? Status { get; set; }

    // Holiday specific
    public int? HolidayId { get; set; }
    public bool IsHoliday { get; set; }
}

public class CalendarFilterDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public List<int>? UserIds { get; set; }
    public List<int>? ActivityTypeIds { get; set; }
    public List<int>? Statuses { get; set; }
    public bool IncludeHolidays { get; set; } = true;
    public bool TeamOnly { get; set; }
}

public class TeamCalendarDto
{
    public List<CalendarEventDto> Events { get; set; } = new();
    public List<UserDto> TeamMembers { get; set; } = new();
    public List<HolidayDto> Holidays { get; set; } = new();
}
