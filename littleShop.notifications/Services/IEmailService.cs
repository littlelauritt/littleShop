namespace littleShop.notifications.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string bodyHtml);
    Task SendWelcomeEmailAsync(string toEmail, string userId);
}