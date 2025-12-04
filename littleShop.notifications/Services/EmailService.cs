using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace littleShop.notifications.Services;

public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _frontendUrl;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;

        // 1. Configuración SMTP
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

        // 2. BÚSQUEDA ROBUSTA DE LA URL DEL FRONTEND
        // Estrategia 1: Variable de entorno directa (la que intentamos antes)
        var url = configuration["FRONTEND_URL"];

        // Estrategia 2: Service Discovery de Aspire (gracias a .WithReference)
        if (string.IsNullOrEmpty(url))
        {
            url = configuration["services:littleshop-frontend:frontend-http:0"];
        }

        // Estrategia 3: Service Discovery alternativo (a veces cambia el nombre interno)
        if (string.IsNullOrEmpty(url))
        {
            url = configuration["services:littleshop-frontend:http:0"];
        }

        // Resultado final
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogError("🛑 ERROR CRÍTICO: No se pudo encontrar la URL del frontend en ninguna configuración. Usando localhost:5173 como último recurso.");
            _frontendUrl = "http://localhost:5173";
        }
        else
        {
            _frontendUrl = url;
            _logger.LogInformation($"✅ URL del Frontend detectada correctamente: {_frontendUrl}");
        }
    }

    public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@")) return;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("LittleShop", "noreply@littleshop.local"));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

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
        var shopLink = _frontendUrl;
        var content = $@"
            <p>Estamos encantados de tenerte con nosotros.</p>
            <p>Ya puedes acceder a tu cuenta y empezar a llenar tu carrito.</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{shopLink}' style='background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Ir a la Tienda</a>
            </div>";

        await SendEmailAsync(toEmail, "¡Bienvenido a LittleShop! 🎉", content);
    }

    public async Task SendVerificationEmailAsync(string toEmail, string userId, string code)
    {
        var verifyLink = $"{_frontendUrl}/verify-email?userId={userId}&code={code}";

        var content = $@"
            <p>Gracias por registrarte. Verifica tu correo para continuar.</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{verifyLink}' style='background-color: #10b981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Verificar mi Email</a>
            </div>
            <p style='font-size: 0.9em; color: #666;'>Si no funciona, copia: <br> 
            <a href='{verifyLink}' style='color: #2563eb;'>{verifyLink}</a></p>";

        await SendEmailAsync(toEmail, "Verifica tu cuenta en LittleShop 🛡️", content);
    }

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