using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Interfaces;
using LeaveManagement.Shared.Common;
using LeaveManagement.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public CalendarController(
        IUnitOfWork unitOfWork,
        IUserService userService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _currentUserService = currentUserService;
    }

    [HttpGet("events")]
    public async Task<ActionResult<ApiResponse<List<CalendarEventDto>>>> GetEvents([FromQuery] CalendarFilterDto filter)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await _userService.GetUserByIdAsync(userId) ?? throw new InvalidOperationException("User not found");

        var events = new List<CalendarEventDto>();

        // Get leave requests
        var requestsQuery = _unitOfWork.LeaveRequests
            .Query()
            .Include(r => r.User)
            .Include(r => r.ActivityType)
            .Where(r => r.StartDate <= filter.EndDate && r.EndDate >= filter.StartDate);

        if (filter.TeamOnly)
        {
            var subordinates = await _userService.GetHierarchyTreeAsync(userId);
            var teamIds = subordinates.Select(s => s.Id).ToList();
            teamIds.Add(userId);
            requestsQuery = requestsQuery.Where(r => teamIds.Contains(r.UserId));
        }
        else if (filter.UserIds != null && filter.UserIds.Any())
        {
            requestsQuery = requestsQuery.Where(r => filter.UserIds.Contains(r.UserId));
        }
        else
        {
            requestsQuery = requestsQuery.Where(r => r.UserId == userId);
        }

        if (filter.ActivityTypeIds != null && filter.ActivityTypeIds.Any())
        {
            requestsQuery = requestsQuery.Where(r => filter.ActivityTypeIds.Contains(r.ActivityTypeId));
        }

        if (filter.Statuses != null && filter.Statuses.Any())
        {
            requestsQuery = requestsQuery.Where(r => filter.Statuses.Contains((int)r.Status));
        }
        else
        {
            // By default, show only approved and pending
            requestsQuery = requestsQuery.Where(r =>
                r.Status == RequestStatus.Approved ||
                r.Status == RequestStatus.Pending);
        }

        var requests = await requestsQuery.ToListAsync();

        events.AddRange(requests.Select(r => new CalendarEventDto
        {
            Id = r.Id,
            Title = $"{r.User?.FullName} - {r.ActivityType?.Name}",
            Start = r.StartDate,
            End = r.EndDate.AddDays(1), // Calendar expects exclusive end date
            Color = r.ActivityType?.Color ?? "#2196F3",
            AllDay = r.TimeTrackingMode == TimeTrackingMode.FullDay,
            EventType = "LeaveRequest",
            RequestId = r.Id,
            UserName = r.User?.FullName,
            ActivityTypeName = r.ActivityType?.Name,
            Status = r.Status.ToString(),
            IsHoliday = false
        }));

        // Get holidays
        if (filter.IncludeHolidays)
        {
            var targetCompanyId = filter.CompanyId ?? user.CompanyId;

            var holidays = await _unitOfWork.Holidays
                .Query()
                .Where(h => h.IsActive &&
                           (h.CompanyId == null || h.CompanyId == targetCompanyId) &&
                           h.Date >= filter.StartDate &&
                           h.Date <= filter.EndDate)
                .ToListAsync();

            events.AddRange(holidays.Select(h => new CalendarEventDto
            {
                Id = h.Id,
                Title = h.Name,
                Start = h.Date,
                End = h.Date.AddDays(1),
                Color = "#E91E63",
                AllDay = true,
                EventType = "Holiday",
                HolidayId = h.Id,
                IsHoliday = true
            }));
        }

        return Ok(ApiResponse<List<CalendarEventDto>>.Ok(events));
    }

    [HttpGet("team")]
    public async Task<ActionResult<ApiResponse<TeamCalendarDto>>> GetTeamCalendar([FromQuery] CalendarFilterDto filter)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await _userService.GetUserByIdAsync(userId) ?? throw new InvalidOperationException("User not found");

        var teamMembers = await _userService.GetSubordinatesForManagerAsync(userId);
        var teamMemberDtos = teamMembers.Select(m => new UserDto
        {
            Id = m.Id,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            Department = m.Department,
            JobTitle = m.JobTitle,
            CompanyId = m.CompanyId,
            IsActive = m.IsActive
        }).ToList();

        filter.TeamOnly = true;
        var eventsResult = await GetEvents(filter);
        var events = eventsResult.Value?.Data ?? new List<CalendarEventDto>();

        var targetCompanyId = filter.CompanyId ?? user.CompanyId;
        var holidays = await _unitOfWork.Holidays
            .Query()
            .Include(h => h.Company)
            .Where(h => h.IsActive &&
                       (h.CompanyId == null || h.CompanyId == targetCompanyId) &&
                       h.Date >= filter.StartDate &&
                       h.Date <= filter.EndDate)
            .ToListAsync();

        var holidayDtos = holidays.Select(h => new HolidayDto
        {
            Id = h.Id,
            CompanyId = h.CompanyId,
            CompanyName = h.Company?.Name,
            Name = h.Name,
            Date = h.Date,
            IsRecurringYearly = h.IsRecurringYearly,
            IsHalfDay = h.IsHalfDay,
            IsActive = h.IsActive
        }).ToList();

        var result = new TeamCalendarDto
        {
            Events = events,
            TeamMembers = teamMemberDtos,
            Holidays = holidayDtos
        };

        return Ok(ApiResponse<TeamCalendarDto>.Ok(result));
    }
}
