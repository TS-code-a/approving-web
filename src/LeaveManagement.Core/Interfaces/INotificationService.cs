using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;

namespace LeaveManagement.Core.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(LeaveRequest request, NotificationTrigger trigger, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task<string> RenderTemplateAsync(NotificationTemplate template, LeaveRequest request, CancellationToken cancellationToken = default);
}
