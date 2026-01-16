using LeaveManagement.Core.Entities;
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
public class HolidaysController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public HolidaysController(
        IUnitOfWork unitOfWork,
        IUserService userService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<HolidayDto>>>> GetHolidays(
        [FromQuery] int? year = null,
        [FromQuery] int? companyId = null)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await _userService.GetUserByIdAsync(userId) ?? throw new InvalidOperationException("User not found");

        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetCompanyId = companyId ?? user.CompanyId;

        var holidays = await _unitOfWork.Holidays
            .Query()
            .Include(h => h.Company)
            .Where(h => h.IsActive &&
                       (h.CompanyId == null || h.CompanyId == targetCompanyId) &&
                       h.Date.Year == targetYear)
            .OrderBy(h => h.Date)
            .ToListAsync();

        var dtos = holidays.Select(h => new HolidayDto
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

        return Ok(ApiResponse<List<HolidayDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<HolidayDto>>> GetHoliday(int id)
    {
        var holiday = await _unitOfWork.Holidays
            .Query()
            .Include(h => h.Company)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (holiday == null)
        {
            return NotFound(ApiResponse<HolidayDto>.Fail("Holiday not found"));
        }

        var dto = new HolidayDto
        {
            Id = holiday.Id,
            CompanyId = holiday.CompanyId,
            CompanyName = holiday.Company?.Name,
            Name = holiday.Name,
            Date = holiday.Date,
            IsRecurringYearly = holiday.IsRecurringYearly,
            IsHalfDay = holiday.IsHalfDay,
            IsActive = holiday.IsActive
        };

        return Ok(ApiResponse<HolidayDto>.Ok(dto));
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<HolidayDto>>> CreateHoliday([FromBody] HolidayCreateDto dto)
    {
        var holiday = new Holiday
        {
            CompanyId = dto.CompanyId,
            Name = dto.Name,
            Date = dto.Date,
            IsRecurringYearly = dto.IsRecurringYearly,
            IsHalfDay = dto.IsHalfDay,
            IsActive = true,
            CreatedBy = _currentUserService.UserName
        };

        var created = await _unitOfWork.Holidays.AddAsync(holiday);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHoliday), new { id = created.Id }, ApiResponse<HolidayDto>.Ok(new HolidayDto
        {
            Id = created.Id,
            CompanyId = created.CompanyId,
            Name = created.Name,
            Date = created.Date,
            IsRecurringYearly = created.IsRecurringYearly,
            IsHalfDay = created.IsHalfDay,
            IsActive = created.IsActive
        }));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<HolidayDto>>> UpdateHoliday(int id, [FromBody] HolidayUpdateDto dto)
    {
        var holiday = await _unitOfWork.Holidays.GetByIdAsync(id);
        if (holiday == null)
        {
            return NotFound(ApiResponse<HolidayDto>.Fail("Holiday not found"));
        }

        holiday.Name = dto.Name;
        holiday.Date = dto.Date;
        holiday.IsRecurringYearly = dto.IsRecurringYearly;
        holiday.IsHalfDay = dto.IsHalfDay;
        holiday.IsActive = dto.IsActive;
        holiday.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.Holidays.UpdateAsync(holiday);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<HolidayDto>.Ok(new HolidayDto
        {
            Id = holiday.Id,
            CompanyId = holiday.CompanyId,
            Name = holiday.Name,
            Date = holiday.Date,
            IsRecurringYearly = holiday.IsRecurringYearly,
            IsHalfDay = holiday.IsHalfDay,
            IsActive = holiday.IsActive
        }));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse>> DeleteHoliday(int id)
    {
        var holiday = await _unitOfWork.Holidays.GetByIdAsync(id);
        if (holiday == null)
        {
            return NotFound(ApiResponse.Fail("Holiday not found"));
        }

        holiday.IsActive = false;
        await _unitOfWork.Holidays.UpdateAsync(holiday);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Holiday deleted"));
    }
}
