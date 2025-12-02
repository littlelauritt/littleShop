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
        _logger.LogInformation($"🔧 [DEBUG SMTP] Cadena recibida: '{connectionString}'");

        try
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback por defecto si Aspire no inyecta nada
                _smtpHost = "localhost";
                _smtpPort = 1025;
            }
            else
            {
                if (!connectionString.Contains("://")) connectionString = $"tcp://{connectionString}";
                var uri = new Uri(connectionString);
                _smtpHost = uri.Host;
                _smtpPort = uri.Port > 0 ? uri.Port : 1025;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error parseando URL. Usando localhost:1025. Error: {ex.Message}");
            _smtpHost = "localhost";
            _smtpPort = 1025;
        }

        _logger.LogInformation($"✅ [CONFIG FINAL] Host: {_smtpHost}, Puerto: {_smtpPort}");
    }

    public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        // 1. PROTECCIÓN: Si el email está vacío o es nulo (pedidos viejos), no hacemos nada.
        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@"))
        {
            _logger.LogWarning($"⚠️ Email omitido: La dirección de correo '{toEmail}' no es válida.");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("LittleShop", "noreply@littleshop.local"));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();

            // INTENTO DE SOLUCIÓN AL ERROR DE PROTOCOLO:
            // A veces 'None' falla si el servidor espera un saludo específico.
            // 'Auto' suele ser más compatible. Si falla, prueba a volver a 'None'.
            await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.Auto);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"✅ Email enviado a {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Fallo crítico enviando email a {toEmail}");
            // No hacemos 'throw' aquí para no bloquear el consumidor si el email falla
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userId)
    {
        var htmlContent = $@"<h1>¡Bienvenido!</h1><p>ID Cliente: {userId}</p>";
        await SendEmailAsync(toEmail, "¡Bienvenido a LittleShop!", htmlContent);
    }
}