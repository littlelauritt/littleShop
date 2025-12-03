using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace littleShop.notifications.Services;

public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        var connectionString = configuration["SMTP_HOST"];

        try
        {
            if (string.IsNullOrEmpty(connectionString)) { _smtpHost = "localhost"; _smtpPort = 1025; }
            else
            {
                if (!connectionString.Contains("://")) connectionString = $"tcp://{connectionString}";
                var uri = new Uri(connectionString);
                _smtpHost = uri.Host;
                _smtpPort = uri.Port > 0 ? uri.Port : 1025;
            }
        }
        catch
        {
            _smtpHost = "localhost"; _smtpPort = 1025;
        }
    }

    public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@")) return;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("LittleShop", "noreply@littleshop.local"));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        // Envolvemos el contenido en una plantilla bonita
        var finalBody = GetEmailTemplate(subject, bodyHtml);

        var bodyBuilder = new BodyBuilder { HtmlBody = finalBody };
        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.None);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation($"✅ Email enviado a {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Fallo al enviar email a {toEmail}");
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userId)
    {
        // YA NO MOSTRAMOS EL ID, solo un mensaje amable.
        var content = @"
            <p>Estamos encantados de tenerte con nosotros.</p>
            <p>Ya puedes acceder a tu cuenta y empezar a llenar tu carrito con los mejores productos tecnológicos.</p>
            <div style='text-align: center; margin: 30px 0;'>
            </div>";

        await SendEmailAsync(toEmail, "¡Bienvenido a LittleShop! 🎉", content);
    }

    // Plantilla base para todos los correos
    private string GetEmailTemplate(string title, string content)
    {
        return $@"
        <div style='font-family: Arial, sans-serif; background-color: #f3f4f6; padding: 40px 0;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                <div style='background-color: #111827; padding: 20px; text-align: center;'>
                    <h1 style='color: #ffffff; margin: 0; font-size: 24px;'>LittleShop 🛍️</h1>
                </div>
                <div style='padding: 30px; color: #374151; line-height: 1.6;'>
                    <h2 style='color: #111827; margin-top: 0;'>{title}</h2>
                    {content}
                    <p style='margin-top: 30px; font-size: 0.9em; color: #6b7280;'>Atentamente,<br>El equipo de LittleShop</p>
                </div>
                <div style='background-color: #f9fafb; padding: 15px; text-align: center; font-size: 0.8em; color: #9ca3af;'>
                    &copy; {DateTime.Now.Year} LittleShop. Todos los derechos reservados.
                </div>
            </div>
        </div>";
    }
}