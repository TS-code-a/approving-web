using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Interfaces;
using LeaveManagement.Shared.Common;
using LeaveManagement.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthController(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto dto)
    {
        // For demo purposes, authenticate by email only
        // In production, implement proper password hashing and validation
        var user = await _unitOfWork.Users
            .Query()
            .Include(u => u.Company)
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.IsActive);

        if (user == null)
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Invalid credentials"));
        }

        var permissions = user.Permissions
            .Where(p => p.IsActive && (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow))
            .Select(p => p.PermissionType.ToString())
            .ToList();

        var token = GenerateJwtToken(user, permissions);
        var expiration = DateTime.UtcNow.AddHours(24);

        var response = new LoginResponseDto
        {
            Token = token,
            Expiration = expiration,
            User = new UserDto
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
                ApprovalLogic = (int)user.ApprovalLogic
            },
            Permissions = permissions
        };

        return Ok(ApiResponse<LoginResponseDto>.Ok(response));
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> RefreshToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("userId")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Invalid token"));
        }

        var user = await _unitOfWork.Users
            .Query()
            .Include(u => u.Company)
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null)
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail("User not found"));
        }

        var permissions = user.Permissions
            .Where(p => p.IsActive && (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow))
            .Select(p => p.PermissionType.ToString())
            .ToList();

        var token = GenerateJwtToken(user, permissions);
        var expiration = DateTime.UtcNow.AddHours(24);

        var response = new LoginResponseDto
        {
            Token = token,
            Expiration = expiration,
            User = new UserDto
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
                ApprovalLogic = (int)user.ApprovalLogic
            },
            Permissions = permissions
        };

        return Ok(ApiResponse<LoginResponseDto>.Ok(response));
    }

    [HttpGet("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        return Ok(ApiResponse.Ok("Token is valid"));
    }

    private string GenerateJwtToken(Core.Entities.UserProfile user, List<string> permissions)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("userId", user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("companyId", user.CompanyId.ToString())
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(ClaimTypes.Role, permission));
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
