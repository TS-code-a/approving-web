using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Extensions;

public static class DbSeeder
{
    public static async Task SeedAsync(LeaveManagementDbContext context)
    {
        if (await context.Companies.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Seed Companies
        var companies = new List<Company>
        {
            new Company
            {
                Name = "Acme Corporation",
                Code = "ACME",
                Description = "Main company",
                IsActive = true,
                TimeZone = "UTC",
                DefaultCurrency = "USD"
            },
            new Company
            {
                Name = "Tech Solutions Ltd",
                Code = "TECH",
                Description = "Technology subsidiary",
                IsActive = true,
                TimeZone = "UTC",
                DefaultCurrency = "USD"
            }
        };

        await context.Companies.AddRangeAsync(companies);
        await context.SaveChangesAsync();

        // Seed Activity Types (Global)
        var activityTypes = new List<ActivityType>
        {
            new ActivityType
            {
                Name = "Vacation",
                Code = "VAC",
                Description = "Annual vacation/holiday leave",
                Color = "#4CAF50",
                Icon = "beach_access",
                IsGlobal = true,
                IsActive = true,
                SortOrder = 1,
                RequiresApproval = true,
                ApprovalWorkflow = ApprovalWorkflowType.SingleLevel,
                DeductsFromBalance = true,
                DefaultAnnualBalance = 20,
                AllowNegativeBalance = false,
                AllowCarryOver = true,
                MaxCarryOverDays = 5,
                TimeTrackingMode = TimeTrackingMode.FullDay,
                NotifyOnSubmit = true,
                NotifyOnApprove = true,
                NotifyOnReject = true,
                AllowCancellation = true,
                CancellationDeadlineHours = 24
            },
            new ActivityType
            {
                Name = "Sick Leave",
                Code = "SICK",
                Description = "Sick day leave",
                Color = "#F44336",
                Icon = "local_hospital",
                IsGlobal = true,
                IsActive = true,
                SortOrder = 2,
                RequiresApproval = true,
                ApprovalWorkflow = ApprovalWorkflowType.SingleLevel,
                DeductsFromBalance = true,
                DefaultAnnualBalance = 10,
                AllowNegativeBalance = false,
                AllowCarryOver = false,
                TimeTrackingMode = TimeTrackingMode.FullDay,
                NotifyOnSubmit = true,
                NotifyOnApprove = true,
                NotifyOnReject = true,
                AllowCancellation = true,
                RequiresAttachment = false
            },
            new ActivityType
            {
                Name = "Work From Home",
                Code = "WFH",
                Description = "Remote work day",
                Color = "#2196F3",
                Icon = "home",
                IsGlobal = true,
                IsActive = true,
                SortOrder = 3,
                RequiresApproval = false,
                ApprovalWorkflow = ApprovalWorkflowType.AutoApprove,
                DeductsFromBalance = false,
                TimeTrackingMode = TimeTrackingMode.FullDay,
                NotifyOnSubmit = true,
                NotifyOnApprove = false,
                NotifyOnReject = false,
                AllowCancellation = true,
                AllowOverlapping = false
            },
            new ActivityType
            {
                Name = "Doctor Appointment",
                Code = "DOC",
                Description = "Medical appointment",
                Color = "#9C27B0",
                Icon = "medical_services",
                IsGlobal = true,
                IsActive = true,
                SortOrder = 4,
                RequiresApproval = true,
                ApprovalWorkflow = ApprovalWorkflowType.SingleLevel,
                DeductsFromBalance = false,
                TimeTrackingMode = TimeTrackingMode.SpecificHours,
                DefaultStartTime = new TimeSpan(9, 0, 0),
                DefaultEndTime = new TimeSpan(12, 0, 0),
                NotifyOnSubmit = true,
                NotifyOnApprove = true,
                NotifyOnReject = true,
                AllowCancellation = true
            },
            new ActivityType
            {
                Name = "Personal Time Off",
                Code = "PTO",
                Description = "Personal time off",
                Color = "#FF9800",
                Icon = "person",
                IsGlobal = true,
                IsActive = true,
                SortOrder = 5,
                RequiresApproval = true,
                ApprovalWorkflow = ApprovalWorkflowType.SingleLevel,
                DeductsFromBalance = true,
                DefaultAnnualBalance = 5,
                AllowNegativeBalance = false,
                AllowCarryOver = false,
                TimeTrackingMode = TimeTrackingMode.FullDay,
                NotifyOnSubmit = true,
                NotifyOnApprove = true,
                NotifyOnReject = true,
                AllowCancellation = true
            },
            new ActivityType
            {
                Name = "Business Trip",
                Code = "TRIP",
                Description = "Business travel",
                Color = "#607D8B",
                Icon = "flight",
                IsGlobal = true,
                IsActive = true,
                SortOrder = 6,
                RequiresApproval = true,
                ApprovalWorkflow = ApprovalWorkflowType.MultiLevel,
                MaxApprovalLevels = 2,
                DeductsFromBalance = false,
                TimeTrackingMode = TimeTrackingMode.FullDay,
                NotifyOnSubmit = true,
                NotifyOnApprove = true,
                NotifyOnReject = true,
                AllowCancellation = true,
                RequiresComment = true
            }
        };

        await context.ActivityTypes.AddRangeAsync(activityTypes);
        await context.SaveChangesAsync();

        // Add custom fields for Business Trip
        var businessTripType = activityTypes.First(a => a.Code == "TRIP");
        var customFields = new List<ActivityField>
        {
            new ActivityField
            {
                ActivityTypeId = businessTripType.Id,
                Name = "Destination",
                Label = "Travel Destination",
                FieldType = FieldType.Text,
                IsRequired = true,
                Placeholder = "Enter destination city/country",
                SortOrder = 1,
                IsActive = true
            },
            new ActivityField
            {
                ActivityTypeId = businessTripType.Id,
                Name = "Purpose",
                Label = "Trip Purpose",
                FieldType = FieldType.TextArea,
                IsRequired = true,
                Placeholder = "Describe the purpose of the trip",
                SortOrder = 2,
                IsActive = true
            },
            new ActivityField
            {
                ActivityTypeId = businessTripType.Id,
                Name = "EstimatedBudget",
                Label = "Estimated Budget",
                FieldType = FieldType.Currency,
                IsRequired = false,
                Placeholder = "0.00",
                SortOrder = 3,
                IsActive = true
            }
        };

        await context.ActivityFields.AddRangeAsync(customFields);

        // Seed Users
        var users = new List<UserProfile>
        {
            new UserProfile
            {
                ExternalUserId = "admin-001",
                Email = "admin@acme.com",
                FirstName = "System",
                LastName = "Admin",
                Department = "IT",
                JobTitle = "System Administrator",
                CompanyId = companies[0].Id,
                IsActive = true,
                ApprovalLogic = ApprovalLogicType.AnyManager
            },
            new UserProfile
            {
                ExternalUserId = "mgr-001",
                Email = "manager@acme.com",
                FirstName = "John",
                LastName = "Manager",
                Department = "Engineering",
                JobTitle = "Engineering Manager",
                CompanyId = companies[0].Id,
                IsActive = true,
                ApprovalLogic = ApprovalLogicType.AnyManager
            },
            new UserProfile
            {
                ExternalUserId = "emp-001",
                Email = "employee@acme.com",
                FirstName = "Jane",
                LastName = "Employee",
                Department = "Engineering",
                JobTitle = "Software Developer",
                CompanyId = companies[0].Id,
                IsActive = true,
                ApprovalLogic = ApprovalLogicType.AnyManager
            },
            new UserProfile
            {
                ExternalUserId = "hr-001",
                Email = "hr@acme.com",
                FirstName = "Sarah",
                LastName = "HR",
                Department = "Human Resources",
                JobTitle = "HR Manager",
                CompanyId = companies[0].Id,
                IsActive = true,
                ApprovalLogic = ApprovalLogicType.AnyManager
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Set up manager relationships
        var managerRelationships = new List<UserManager>
        {
            new UserManager
            {
                UserId = users[2].Id, // Employee
                ManagerId = users[1].Id, // Manager
                Level = 1,
                IsPrimary = true,
                IsActive = true
            },
            new UserManager
            {
                UserId = users[1].Id, // Manager
                ManagerId = users[0].Id, // Admin
                Level = 1,
                IsPrimary = true,
                IsActive = true
            }
        };

        await context.UserManagers.AddRangeAsync(managerRelationships);

        // Set up permissions
        var permissions = new List<UserPermission>
        {
            new UserPermission
            {
                UserId = users[0].Id,
                PermissionType = PermissionType.SystemAdmin,
                IsActive = true
            },
            new UserPermission
            {
                UserId = users[1].Id,
                PermissionType = PermissionType.Manager,
                IsActive = true
            },
            new UserPermission
            {
                UserId = users[3].Id,
                PermissionType = PermissionType.HRViewer,
                TargetCompanyId = companies[0].Id,
                IsActive = true
            }
        };

        await context.UserPermissions.AddRangeAsync(permissions);

        // Initialize balances for all users
        var currentYear = DateTime.UtcNow.Year;
        var balances = new List<UserBalance>();

        foreach (var user in users)
        {
            foreach (var activityType in activityTypes.Where(a => a.DeductsFromBalance && a.DefaultAnnualBalance.HasValue))
            {
                balances.Add(new UserBalance
                {
                    UserId = user.Id,
                    ActivityTypeId = activityType.Id,
                    Year = currentYear,
                    TotalDays = activityType.DefaultAnnualBalance!.Value,
                    UsedDays = 0,
                    PendingDays = 0,
                    CarriedOverDays = 0,
                    AdjustmentDays = 0
                });
            }
        }

        await context.UserBalances.AddRangeAsync(balances);

        // Add notification templates
        var notificationTemplates = new List<NotificationTemplate>
        {
            new NotificationTemplate
            {
                ActivityTypeId = null, // Global template
                Trigger = NotificationTrigger.OnSubmit,
                Subject = "Leave Request Submitted - {{RequestNumber}}",
                Body = @"<h2>Leave Request Submitted</h2>
<p>Dear {{UserFirstName}},</p>
<p>Your leave request has been submitted successfully.</p>
<table>
<tr><td><strong>Request Number:</strong></td><td>{{RequestNumber}}</td></tr>
<tr><td><strong>Type:</strong></td><td>{{ActivityType}}</td></tr>
<tr><td><strong>From:</strong></td><td>{{StartDate}}</td></tr>
<tr><td><strong>To:</strong></td><td>{{EndDate}}</td></tr>
<tr><td><strong>Total Days:</strong></td><td>{{TotalDays}}</td></tr>
</table>
<p>You will be notified once your request is processed.</p>",
                IsHtml = true,
                IsActive = true,
                SendToRequester = true,
                SendToApprovers = true
            },
            new NotificationTemplate
            {
                ActivityTypeId = null,
                Trigger = NotificationTrigger.OnApprove,
                Subject = "Leave Request Approved - {{RequestNumber}}",
                Body = @"<h2>Leave Request Approved</h2>
<p>Dear {{UserFirstName}},</p>
<p>Your leave request has been <strong style='color: green;'>approved</strong>.</p>
<table>
<tr><td><strong>Request Number:</strong></td><td>{{RequestNumber}}</td></tr>
<tr><td><strong>Type:</strong></td><td>{{ActivityType}}</td></tr>
<tr><td><strong>From:</strong></td><td>{{StartDate}}</td></tr>
<tr><td><strong>To:</strong></td><td>{{EndDate}}</td></tr>
<tr><td><strong>Total Days:</strong></td><td>{{TotalDays}}</td></tr>
</table>",
                IsHtml = true,
                IsActive = true,
                SendToRequester = true,
                SendToApprovers = false,
                SendToHR = true
            },
            new NotificationTemplate
            {
                ActivityTypeId = null,
                Trigger = NotificationTrigger.OnReject,
                Subject = "Leave Request Rejected - {{RequestNumber}}",
                Body = @"<h2>Leave Request Rejected</h2>
<p>Dear {{UserFirstName}},</p>
<p>Your leave request has been <strong style='color: red;'>rejected</strong>.</p>
<table>
<tr><td><strong>Request Number:</strong></td><td>{{RequestNumber}}</td></tr>
<tr><td><strong>Type:</strong></td><td>{{ActivityType}}</td></tr>
<tr><td><strong>From:</strong></td><td>{{StartDate}}</td></tr>
<tr><td><strong>To:</strong></td><td>{{EndDate}}</td></tr>
</table>
<p>Please contact your manager for more information.</p>",
                IsHtml = true,
                IsActive = true,
                SendToRequester = true
            }
        };

        await context.NotificationTemplates.AddRangeAsync(notificationTemplates);

        // Add some holidays
        var holidays = new List<Holiday>
        {
            new Holiday
            {
                CompanyId = null, // Global holiday
                Name = "New Year's Day",
                Date = new DateTime(currentYear, 1, 1),
                IsRecurringYearly = true,
                IsActive = true
            },
            new Holiday
            {
                CompanyId = null,
                Name = "Christmas Day",
                Date = new DateTime(currentYear, 12, 25),
                IsRecurringYearly = true,
                IsActive = true
            },
            new Holiday
            {
                CompanyId = null,
                Name = "Independence Day",
                Date = new DateTime(currentYear, 7, 4),
                IsRecurringYearly = true,
                IsActive = true
            }
        };

        await context.Holidays.AddRangeAsync(holidays);

        await context.SaveChangesAsync();
    }
}
