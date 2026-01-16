using LeaveManagement.Core.Entities;
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
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly IBalanceService _balanceService;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(
        IUnitOfWork unitOfWork,
        IUserService userService,
        IBalanceService balanceService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _balanceService = balanceService;
        _currentUserService = currentUserService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await _unitOfWork.Users
            .Query()
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("User not found"));
        }

        return Ok(ApiResponse<UserDto>.Ok(MapToDto(user)));
    }

    [HttpGet]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? companyId = null)
    {
        var usersQuery = _unitOfWork.Users.Query().Include(u => u.Company);

        if (companyId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.CompanyId == companyId.Value);
        }

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            usersQuery = usersQuery.Where(u =>
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm));
        }

        var totalCount = await usersQuery.CountAsync();

        var users = await usersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = users.Select(MapToDto).ToList();

        var result = new PagedResult<UserDto>(dtos, totalCount, query.PageNumber, query.PageSize);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        // Users can view themselves, or admins/managers can view others
        if (id != userId)
        {
            var isAdmin = await _userService.HasPermissionAsync(userId, PermissionType.SystemAdmin);
            var isManager = await _userService.HasPermissionAsync(userId, PermissionType.Manager);
            var isHR = await _userService.HasPermissionAsync(userId, PermissionType.HRViewer);

            if (!isAdmin && !isManager && !isHR)
            {
                return Forbid();
            }
        }

        var user = await _unitOfWork.Users
            .Query()
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("User not found"));
        }

        return Ok(ApiResponse<UserDto>.Ok(MapToDto(user)));
    }

    [HttpGet("{id}/managers")]
    public async Task<ActionResult<ApiResponse<List<UserManagerDto>>>> GetUserManagers(int id)
    {
        var managers = await _unitOfWork.UserManagers
            .Query()
            .Include(um => um.Manager)
            .Include(um => um.User)
            .Where(um => um.UserId == id && um.IsActive)
            .ToListAsync();

        var dtos = managers.Select(um => new UserManagerDto
        {
            Id = um.Id,
            UserId = um.UserId,
            UserName = um.User?.FullName ?? string.Empty,
            ManagerId = um.ManagerId,
            ManagerName = um.Manager?.FullName ?? string.Empty,
            Level = um.Level,
            IsPrimary = um.IsPrimary,
            IsActive = um.IsActive
        }).ToList();

        return Ok(ApiResponse<List<UserManagerDto>>.Ok(dtos));
    }

    [HttpPost("{id}/managers")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<UserManagerDto>>> AddManager(int id, [FromBody] UserManagerDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse<UserManagerDto>.Fail("User not found"));
        }

        var manager = await _unitOfWork.Users.GetByIdAsync(dto.ManagerId);
        if (manager == null)
        {
            return NotFound(ApiResponse<UserManagerDto>.Fail("Manager not found"));
        }

        var existing = await _unitOfWork.UserManagers.AnyAsync(
            um => um.UserId == id && um.ManagerId == dto.ManagerId && um.IsActive);

        if (existing)
        {
            return BadRequest(ApiResponse<UserManagerDto>.Fail("Manager relationship already exists"));
        }

        var userManager = new UserManager
        {
            UserId = id,
            ManagerId = dto.ManagerId,
            Level = dto.Level,
            IsPrimary = dto.IsPrimary,
            IsActive = true,
            CreatedBy = _currentUserService.UserName
        };

        var created = await _unitOfWork.UserManagers.AddAsync(userManager);
        await _unitOfWork.SaveChangesAsync();

        dto.Id = created.Id;
        dto.UserName = user.FullName;
        dto.ManagerName = manager.FullName;

        return Ok(ApiResponse<UserManagerDto>.Ok(dto));
    }

    [HttpDelete("{id}/managers/{managerId}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse>> RemoveManager(int id, int managerId)
    {
        var relationship = await _unitOfWork.UserManagers.FirstOrDefaultAsync(
            um => um.UserId == id && um.ManagerId == managerId && um.IsActive);

        if (relationship == null)
        {
            return NotFound(ApiResponse.Fail("Manager relationship not found"));
        }

        relationship.IsActive = false;
        await _unitOfWork.UserManagers.UpdateAsync(relationship);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Manager removed"));
    }

    [HttpGet("{id}/subordinates")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetSubordinates(int id)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        // Can only view subordinates if it's your own hierarchy or you're admin
        if (id != userId)
        {
            var isAdmin = await _userService.HasPermissionAsync(userId, PermissionType.SystemAdmin);
            if (!isAdmin)
            {
                return Forbid();
            }
        }

        var subordinates = await _userService.GetSubordinatesForManagerAsync(id);
        var dtos = subordinates.Select(u => MapToDto(u)).ToList();

        return Ok(ApiResponse<List<UserDto>>.Ok(dtos));
    }

    [HttpGet("{id}/balances")]
    public async Task<ActionResult<ApiResponse<List<UserBalanceDto>>>> GetUserBalances(int id, [FromQuery] int? year = null)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        // Users can view their own balances, or admins/HR can view others
        if (id != userId)
        {
            var isAdmin = await _userService.HasPermissionAsync(userId, PermissionType.SystemAdmin);
            var isHR = await _userService.HasPermissionAsync(userId, PermissionType.HRViewer);

            if (!isAdmin && !isHR)
            {
                return Forbid();
            }
        }

        var targetYear = year ?? DateTime.UtcNow.Year;
        var balances = await _unitOfWork.UserBalances
            .Query()
            .Include(b => b.ActivityType)
            .Include(b => b.User)
            .Where(b => b.UserId == id && b.Year == targetYear)
            .ToListAsync();

        var dtos = balances.Select(b => new UserBalanceDto
        {
            Id = b.Id,
            UserId = b.UserId,
            UserName = b.User?.FullName ?? string.Empty,
            ActivityTypeId = b.ActivityTypeId,
            ActivityTypeName = b.ActivityType?.Name ?? string.Empty,
            ActivityTypeColor = b.ActivityType?.Color,
            Year = b.Year,
            TotalDays = b.TotalDays,
            UsedDays = b.UsedDays,
            PendingDays = b.PendingDays,
            CarriedOverDays = b.CarriedOverDays,
            AdjustmentDays = b.AdjustmentDays
        }).ToList();

        return Ok(ApiResponse<List<UserBalanceDto>>.Ok(dtos));
    }

    [HttpPost("{id}/balances/adjust")]
    [Authorize(Policy = "RequireHRRole")]
    public async Task<ActionResult<ApiResponse>> AdjustBalance(int id, [FromBody] BalanceAdjustmentDto dto)
    {
        await _balanceService.AdjustBalanceAsync(
            id,
            dto.ActivityTypeId,
            dto.Year,
            dto.AdjustmentDays,
            dto.Reason);

        return Ok(ApiResponse.Ok("Balance adjusted"));
    }

    [HttpGet("{id}/permissions")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<List<UserPermissionDto>>>> GetUserPermissions(int id)
    {
        var permissions = await _unitOfWork.UserPermissions
            .Query()
            .Include(p => p.User)
            .Include(p => p.TargetCompany)
            .Where(p => p.UserId == id && p.IsActive)
            .ToListAsync();

        var dtos = permissions.Select(p => new UserPermissionDto
        {
            Id = p.Id,
            UserId = p.UserId,
            UserName = p.User?.FullName ?? string.Empty,
            PermissionType = (int)p.PermissionType,
            PermissionTypeName = p.PermissionType.ToString(),
            TargetCompanyId = p.TargetCompanyId,
            TargetCompanyName = p.TargetCompany?.Name,
            ExpiresAt = p.ExpiresAt,
            IsActive = p.IsActive
        }).ToList();

        return Ok(ApiResponse<List<UserPermissionDto>>.Ok(dtos));
    }

    [HttpPost("{id}/permissions")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<UserPermissionDto>>> AddPermission(int id, [FromBody] UserPermissionDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse<UserPermissionDto>.Fail("User not found"));
        }

        var permission = new UserPermission
        {
            UserId = id,
            PermissionType = (PermissionType)dto.PermissionType,
            TargetCompanyId = dto.TargetCompanyId,
            ExpiresAt = dto.ExpiresAt,
            IsActive = true,
            CreatedBy = _currentUserService.UserName
        };

        var created = await _unitOfWork.UserPermissions.AddAsync(permission);
        await _unitOfWork.SaveChangesAsync();

        dto.Id = created.Id;
        dto.UserName = user.FullName;
        dto.PermissionTypeName = permission.PermissionType.ToString();

        return Ok(ApiResponse<UserPermissionDto>.Ok(dto));
    }

    [HttpDelete("{id}/permissions/{permissionId}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse>> RemovePermission(int id, int permissionId)
    {
        var permission = await _unitOfWork.UserPermissions.FirstOrDefaultAsync(
            p => p.Id == permissionId && p.UserId == id);

        if (permission == null)
        {
            return NotFound(ApiResponse.Fail("Permission not found"));
        }

        permission.IsActive = false;
        await _unitOfWork.UserPermissions.UpdateAsync(permission);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Permission removed"));
    }

    private static UserDto MapToDto(UserProfile user)
    {
        return new UserDto
        {
            Id = user.Id,
            ExternalUserId = user.ExternalUserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Department = user.Department,
            JobTitle = user.JobTitle,
            CompanyId = user.CompanyId,
            CompanyName = user.Company?.Name,
            IsActive = user.IsActive,
            ApprovalLogic = (int)user.ApprovalLogic,
            HireDate = user.HireDate
        };
    }
}
