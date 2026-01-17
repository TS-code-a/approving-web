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
public class CompaniesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CompaniesController(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CompanyDto>>>> GetCompanies()
    {
        var companies = await _unitOfWork.Companies
            .Query()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var dtos = companies.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<CompanyDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> GetCompany(int id)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound(ApiResponse<CompanyDto>.Fail("Company not found"));
        }

        return Ok(ApiResponse<CompanyDto>.Ok(MapToDto(company)));
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> CreateCompany([FromBody] CompanyCreateDto dto)
    {
        var existingCode = await _unitOfWork.Companies.AnyAsync(c => c.Code == dto.Code);
        if (existingCode)
        {
            return BadRequest(ApiResponse<CompanyDto>.Fail("Company code already exists"));
        }

        var company = new Company
        {
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            TimeZone = dto.TimeZone,
            DefaultCurrency = dto.DefaultCurrency,
            IsActive = true,
            CreatedBy = _currentUserService.UserName
        };

        var created = await _unitOfWork.Companies.AddAsync(company);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCompany), new { id = created.Id }, ApiResponse<CompanyDto>.Ok(MapToDto(created)));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> UpdateCompany(int id, [FromBody] CompanyUpdateDto dto)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound(ApiResponse<CompanyDto>.Fail("Company not found"));
        }

        company.Name = dto.Name;
        company.Description = dto.Description;
        company.TimeZone = dto.TimeZone;
        company.DefaultCurrency = dto.DefaultCurrency;
        company.IsActive = dto.IsActive;
        company.UpdatedBy = _currentUserService.UserName;

        await _unitOfWork.Companies.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<CompanyDto>.Ok(MapToDto(company)));
    }

    [HttpGet("{id}/relationships")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<List<CompanyRelationshipDto>>>> GetCompanyRelationships(int id)
    {
        var relationships = await _unitOfWork.CompanyRelationships
            .Query()
            .Include(r => r.SourceCompany)
            .Include(r => r.TargetCompany)
            .Where(r => r.SourceCompanyId == id || r.TargetCompanyId == id)
            .ToListAsync();

        var dtos = relationships.Select(r => new CompanyRelationshipDto
        {
            Id = r.Id,
            SourceCompanyId = r.SourceCompanyId,
            SourceCompanyName = r.SourceCompany?.Name ?? string.Empty,
            TargetCompanyId = r.TargetCompanyId,
            TargetCompanyName = r.TargetCompany?.Name ?? string.Empty,
            CanViewRequests = r.CanViewRequests,
            CanApproveRequests = r.CanApproveRequests,
            IsActive = r.IsActive
        }).ToList();

        return Ok(ApiResponse<List<CompanyRelationshipDto>>.Ok(dtos));
    }

    [HttpPost("relationships")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ApiResponse<CompanyRelationshipDto>>> CreateRelationship([FromBody] CompanyRelationshipDto dto)
    {
        var existing = await _unitOfWork.CompanyRelationships.AnyAsync(
            r => r.SourceCompanyId == dto.SourceCompanyId && r.TargetCompanyId == dto.TargetCompanyId);

        if (existing)
        {
            return BadRequest(ApiResponse<CompanyRelationshipDto>.Fail("Relationship already exists"));
        }

        var relationship = new CompanyRelationship
        {
            SourceCompanyId = dto.SourceCompanyId,
            TargetCompanyId = dto.TargetCompanyId,
            CanViewRequests = dto.CanViewRequests,
            CanApproveRequests = dto.CanApproveRequests,
            IsActive = true,
            CreatedBy = _currentUserService.UserName
        };

        var created = await _unitOfWork.CompanyRelationships.AddAsync(relationship);
        await _unitOfWork.SaveChangesAsync();

        dto.Id = created.Id;
        return Ok(ApiResponse<CompanyRelationshipDto>.Ok(dto));
    }

    private static CompanyDto MapToDto(Company company)
    {
        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Code = company.Code,
            Description = company.Description,
            IsActive = company.IsActive,
            TimeZone = company.TimeZone,
            DefaultCurrency = company.DefaultCurrency
        };
    }
}
