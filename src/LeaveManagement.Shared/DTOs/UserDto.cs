using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Shared.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string ExternalUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public bool IsActive { get; set; }
    public int ApprovalLogic { get; set; }
    public DateTime? HireDate { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

public class UserCreateDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }

    [Required]
    public int CompanyId { get; set; }

    public int ApprovalLogic { get; set; }
    public DateTime? HireDate { get; set; }
}

public class UserUpdateDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; }
    public int ApprovalLogic { get; set; }
}

public class UserManagerDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public class UserPermissionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int PermissionType { get; set; }
    public string PermissionTypeName { get; set; } = string.Empty;
    public int? TargetCompanyId { get; set; }
    public string? TargetCompanyName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}
