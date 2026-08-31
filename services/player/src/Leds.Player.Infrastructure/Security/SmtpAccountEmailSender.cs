using System.Net;
using System.Net.Mail;
using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Leds.Player.Infrastructure.Security;

public sealed class SmtpAccountEmailSender : IAccountEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpAccountEmailSender> _logger;

    public SmtpAccountEmailSender(
        IConfiguration configuration,
        ILogger<SmtpAccountEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(
        EmailAddress recipient,
        string rawToken,
        CancellationToken cancellationToken) =>
        SendAsync(
            recipient,
            "Confirmez votre adresse — L'épopée des silences",
            "verify-email",
            rawToken,
            cancellationToken);

    public Task SendPasswordResetEmailAsync(
        EmailAddress recipient,
        string rawToken,
        CancellationToken cancellationToken) =>
        SendAsync(
            recipient,
            "Réinitialisez votre mot de passe — L'épopée des silences",
            "password-reset",
            rawToken,
            cancellationToken);

    private async Task SendAsync(
        EmailAddress recipient,
        string subject,
        string route,
        string rawToken,
        CancellationToken cancellationToken)
    {
        var baseUrl = (_configuration["Authentication:Email:PublicClientBaseUrl"] ?? "http://localhost:5173")
            .TrimEnd('/');
        var actionUrl = $"{baseUrl}/{route}?token={Uri.EscapeDataString(rawToken)}";
        var mode = _configuration["Authentication:Email:Mode"] ?? "Smtp";

        if (string.Equals(mode, "Log", StringComparison.OrdinalIgnoreCase))
        {
            // Authentication links are bearer credentials. Even in Development they must
            // never enter application logs, traces or centralized observability systems.
            _logger.LogInformation(
                "Development account email suppressed for {Recipient}: {Subject}. Configure SMTP to receive the one-time link.",
                recipient.Value,
                subject);
            return;
        }

        var host = _configuration["Authentication:Email:Smtp:Host"];
        var from = _configuration["Authentication:Email:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("SMTP host and Authentication:Email:From must be configured.");

        var port = _configuration.GetValue("Authentication:Email:Smtp:Port", 587);
        var enableSsl = _configuration.GetValue("Authentication:Email:Smtp:EnableSsl", true);
        var username = _configuration["Authentication:Email:Smtp:Username"];
        var password = _configuration["Authentication:Email:Smtp:Password"];

        using var message = new MailMessage(from, recipient.Value)
        {
            Subject = subject,
            Body = $"Ouvrez ce lien à usage unique : {actionUrl}\n\nSi vous n'êtes pas à l'origine de cette demande, ignorez ce message.",
            IsBodyHtml = false
        };
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(username)
        };
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
