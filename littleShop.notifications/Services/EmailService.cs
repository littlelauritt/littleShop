using MailKit.Net.Smtp;
using MimeKit;

namespace littleShop.notifications.Services;

public class EmailService : IEmailService // Asegúrate de que implementa la interfaz
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly ILogger<EmailService> _logger; // Añadimos Logger para ver qué pasa

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;

        // 1. Obtenemos la cadena cruda desde Aspire
        var connectionString = configuration["SMTP_HOST"];

        _logger.LogInformation($"🔧 [DEBUG SMTP] Cadena recibida de Aspire: '{connectionString}'");

        try
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("La cadena de conexión SMTP es nula.");
            }

            // 2. Si no tiene protocolo (://), se lo ponemos para que la clase Uri funcione
            if (!connectionString.Contains("://"))
            {
                connectionString = $"tcp://{connectionString}";
            }

            // 3. Parseamos
            var uri = new Uri(connectionString);
            _smtpHost = uri.Host;
            _smtpPort = uri.Port;

            // 4. Corrección de emergencia: Si el puerto no viene (-1), usamos el default de MailDev
            if (_smtpPort <= 0)
            {
                _smtpPort = 1025;
                _logger.LogWarning("⚠️ No se detectó puerto en la URL. Usando puerto por defecto: 1025");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error parseando URL SMTP. Usando localhost:1025. Error: {ex.Message}");
            _smtpHost = "localhost";
            _smtpPort = 1025;
        }

        _logger.LogInformation($"✅ [CONFIG SMTP] Host: {_smtpHost}, Port: {_smtpPort}");
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userId)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("LittleShop", "no-reply@littleshop.local"));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = "¡Bienvenido a LittleShop! 🎉";

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = $@"
            <div style='font-family: sans-serif; padding: 20px; border: 1px solid #ccc;'>
                <h1 style='color: #4F46E5;'>¡Hola! 👋</h1>
                <p>Gracias por registrarte en nuestra tienda.</p>
                <p>Tu ID de cliente es: <strong>{userId}</strong></p>
                <br>
                <p>Atentamente,<br>El equipo de LittleShop</p>
            </div>";

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        // Conectamos
        await client.ConnectAsync(_smtpHost, _smtpPort, false);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}