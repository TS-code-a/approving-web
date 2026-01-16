using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly LeaveManagementDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(LeaveManagementDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendNotificationAsync(LeaveRequest request, NotificationTrigger trigger, CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t =>
                (t.ActivityTypeId == null || t.ActivityTypeId == request.ActivityTypeId) &&
                t.Trigger == trigger &&
                t.IsActive,
                cancellationToken);

        if (template == null)
        {
            _logger.LogWarning("No notification template found for ActivityTypeId {ActivityTypeId} and Trigger {Trigger}",
                request.ActivityTypeId, trigger);
            return;
        }

        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null) return;

        var renderedBody = await RenderTemplateAsync(template, request, cancellationToken);
        var renderedSubject = RenderSubjectTemplate(template.Subject, request, user);

        var recipients = new List<string>();

        if (template.SendToRequester)
        {
            recipients.Add(user.Email);
        }

        if (template.SendToApprovers)
        {
            var approverEmails = await _context.RequestApprovals
                .Where(a => a.RequestId == request.Id)
                .Include(a => a.Approver)
                .Select(a => a.Approver.Email)
                .ToListAsync(cancellationToken);

            recipients.AddRange(approverEmails);
        }

        if (template.SendToHR)
        {
            var hrEmails = await _context.UserPermissions
                .Where(p => p.PermissionType == PermissionType.HRViewer &&
                           (p.TargetCompanyId == null || p.TargetCompanyId == user.CompanyId) &&
                           p.IsActive)
                .Include(p => p.User)
                .Select(p => p.User.Email)
                .ToListAsync(cancellationToken);

            recipients.AddRange(hrEmails);
        }

        foreach (var recipient in recipients.Distinct())
        {
            await SendEmailAsync(recipient, renderedSubject, renderedBody, template.IsHtml, cancellationToken);
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would use an email service like SendGrid, SMTP, etc.
        // For now, we'll just log the email
        _logger.LogInformation(
            "Sending email to {Recipient} with subject: {Subject}",
            to,
            subject);

        // TODO: Implement actual email sending
        // Example with SendGrid:
        // var client = new SendGridClient(_apiKey);
        // var msg = new SendGridMessage { ... };
        // await client.SendEmailAsync(msg, cancellationToken);

        await Task.CompletedTask;
    }

    public async Task<string> RenderTemplateAsync(NotificationTemplate template, LeaveRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        var activityType = await _context.ActivityTypes
            .FirstOrDefaultAsync(a => a.Id == request.ActivityTypeId, cancellationToken);

        if (user == null || activityType == null)
        {
            return template.Body;
        }

        var body = template.Body;

        // Replace placeholders
        body = body.Replace("{{UserName}}", user.FullName);
        body = body.Replace("{{UserFirstName}}", user.FirstName);
        body = body.Replace("{{UserLastName}}", user.LastName);
        body = body.Replace("{{UserEmail}}", user.Email);
        body = body.Replace("{{UserDepartment}}", user.Department ?? "N/A");
        body = body.Replace("{{CompanyName}}", user.Company?.Name ?? "N/A");

        body = body.Replace("{{ActivityType}}", activityType.Name);
        body = body.Replace("{{RequestNumber}}", request.RequestNumber);
        body = body.Replace("{{StartDate}}", request.StartDate.ToString("yyyy-MM-dd"));
        body = body.Replace("{{EndDate}}", request.EndDate.ToString("yyyy-MM-dd"));
        body = body.Replace("{{TotalDays}}", request.TotalDays.ToString("0.##"));
        body = body.Replace("{{Status}}", request.Status.ToString());
        body = body.Replace("{{Reason}}", request.Reason ?? "N/A");

        body = body.Replace("{{SubmittedDate}}", request.SubmittedAt?.ToString("yyyy-MM-dd HH:mm") ?? "N/A");
        body = body.Replace("{{ProcessedDate}}", request.ProcessedAt?.ToString("yyyy-MM-dd HH:mm") ?? "N/A");

        return body;
    }

    private string RenderSubjectTemplate(string subject, LeaveRequest request, UserProfile user)
    {
        var result = subject;
        result = result.Replace("{{UserName}}", user.FullName);
        result = result.Replace("{{RequestNumber}}", request.RequestNumber);
        result = result.Replace("{{Status}}", request.Status.ToString());
        return result;
    }
}
