using LeaveManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Data;

public class LeaveManagementDbContext : DbContext
{
    public LeaveManagementDbContext(DbContextOptions<LeaveManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyRelationship> CompanyRelationships => Set<CompanyRelationship>();
    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<UserManager> UserManagers => Set<UserManager>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<UserBalance> UserBalances => Set<UserBalance>();
    public DbSet<ProxyApprover> ProxyApprovers => Set<ProxyApprover>();
    public DbSet<ActivityType> ActivityTypes => Set<ActivityType>();
    public DbSet<ActivityField> ActivityFields => Set<ActivityField>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<RequestApproval> RequestApprovals => Set<RequestApproval>();
    public DbSet<RequestFieldValue> RequestFieldValues => Set<RequestFieldValue>();
    public DbSet<RequestComment> RequestComments => Set<RequestComment>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // CompanyRelationship
        modelBuilder.Entity<CompanyRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.SourceCompany)
                .WithMany(c => c.SourceRelationships)
                .HasForeignKey(e => e.SourceCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TargetCompany)
                .WithMany(c => c.TargetRelationships)
                .HasForeignKey(e => e.TargetCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.SourceCompanyId, e.TargetCompanyId }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // UserProfile
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ExternalUserId).HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.ExternalUserId);
            entity.HasOne(e => e.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(e => e.FullName);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // UserManager
        modelBuilder.Entity<UserManager>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.ManagerRelationships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Manager)
                .WithMany(u => u.SubordinateRelationships)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.ManagerId }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // UserPermission
        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Permissions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.TargetCompany)
                .WithMany()
                .HasForeignKey(e => e.TargetCompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // UserBalance
        modelBuilder.Entity<UserBalance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Balances)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ActivityType)
                .WithMany(a => a.Balances)
                .HasForeignKey(e => e.ActivityTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.ActivityTypeId, e.Year }).IsUnique();
            entity.Property(e => e.TotalDays).HasPrecision(10, 2);
            entity.Property(e => e.UsedDays).HasPrecision(10, 2);
            entity.Property(e => e.PendingDays).HasPrecision(10, 2);
            entity.Property(e => e.CarriedOverDays).HasPrecision(10, 2);
            entity.Property(e => e.AdjustmentDays).HasPrecision(10, 2);
            entity.Ignore(e => e.AvailableDays);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ProxyApprover
        modelBuilder.Entity<ProxyApprover>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.OriginalApprover)
                .WithMany(u => u.ProxyApproversFor)
                .HasForeignKey(e => e.OriginalApproverId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ProxyUser)
                .WithMany(u => u.ActingAsProxy)
                .HasForeignKey(e => e.ProxyUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ActivityType
        modelBuilder.Entity<ActivityType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.Icon).HasMaxLength(100);
            entity.HasIndex(e => e.Code);
            entity.HasOne(e => e.Company)
                .WithMany(c => c.ActivityTypes)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.DefaultAnnualBalance).HasPrecision(10, 2);
            entity.Property(e => e.MaxCarryOverDays).HasPrecision(10, 2);
            entity.Property(e => e.MinDuration).HasPrecision(10, 2);
            entity.Property(e => e.MaxDuration).HasPrecision(10, 2);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ActivityField
        modelBuilder.Entity<ActivityField>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.ActivityType)
                .WithMany(a => a.CustomFields)
                .HasForeignKey(e => e.ActivityTypeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // LeaveRequest
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.RequestNumber).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.Requests)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ActivityType)
                .WithMany(a => a.Requests)
                .HasForeignKey(e => e.ActivityTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TotalDays).HasPrecision(10, 2);
            entity.Property(e => e.TotalHours).HasPrecision(10, 2);
            entity.HasIndex(e => new { e.UserId, e.StartDate, e.EndDate });
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // RequestApproval
        modelBuilder.Entity<RequestApproval>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Request)
                .WithMany(r => r.Approvals)
                .HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Approver)
                .WithMany(u => u.Approvals)
                .HasForeignKey(e => e.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ProxyApprover)
                .WithMany()
                .HasForeignKey(e => e.ProxyApproverId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.RequestId, e.ApproverId });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // RequestFieldValue
        modelBuilder.Entity<RequestFieldValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Request)
                .WithMany(r => r.FieldValues)
                .HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ActivityField)
                .WithMany(f => f.FieldValues)
                .HasForeignKey(e => e.ActivityFieldId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // RequestComment
        modelBuilder.Entity<RequestComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Comment).IsRequired();
            entity.HasOne(e => e.Request)
                .WithMany(r => r.Comments)
                .HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // NotificationTemplate
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Body).IsRequired();
            entity.HasOne(e => e.ActivityType)
                .WithMany(a => a.NotificationTemplates)
                .HasForeignKey(e => e.ActivityTypeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Holiday
        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.CompanyId, e.Date });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
