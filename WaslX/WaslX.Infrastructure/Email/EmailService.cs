using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using WaslX.Infrastructure.Settings;

namespace WaslX.Infrastructure.Email;

public class EmailService(IOptions<MailSettings> options, ILogger<EmailService> logger) : IEmailSender
{
    private readonly MailSettings _mailSettings = options.Value;
    private readonly ILogger<EmailService> _logger = logger;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage
        {
            Sender = MailboxAddress.Parse(_mailSettings.Mail),
            Subject = subject
        };

        message.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();

        using var client = new SmtpClient();

        _logger.LogInformation("Trying to send email to {email}", email);

        await client.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

