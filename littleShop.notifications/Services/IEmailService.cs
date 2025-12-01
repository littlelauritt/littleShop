namespace littleShop.notifications.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string userId);
}