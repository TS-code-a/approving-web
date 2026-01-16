using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Shared.DTOs;

public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? TimeZone { get; set; }
    public string? DefaultCurrency { get; set; }
}

public class CompanyCreateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? TimeZone { get; set; }
    public string? DefaultCurrency { get; set; }
}

public class CompanyUpdateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? TimeZone { get; set; }
    public string? DefaultCurrency { get; set; }
}

public class CompanyRelationshipDto
{
    public int Id { get; set; }
    public int SourceCompanyId { get; set; }
    public string SourceCompanyName { get; set; } = string.Empty;
    public int TargetCompanyId { get; set; }
    public string TargetCompanyName { get; set; } = string.Empty;
    public bool CanViewRequests { get; set; }
    public bool CanApproveRequests { get; set; }
    public bool IsActive { get; set; }
}
