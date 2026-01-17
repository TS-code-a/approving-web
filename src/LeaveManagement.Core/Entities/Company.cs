namespace LeaveManagement.Core.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? TimeZone { get; set; }
    public string? DefaultCurrency { get; set; }

    // Navigation properties
    public virtual ICollection<UserProfile> Users { get; set; } = new List<UserProfile>();
    public virtual ICollection<CompanyRelationship> SourceRelationships { get; set; } = new List<CompanyRelationship>();
    public virtual ICollection<CompanyRelationship> TargetRelationships { get; set; } = new List<CompanyRelationship>();
    public virtual ICollection<ActivityType> ActivityTypes { get; set; } = new List<ActivityType>();
}
