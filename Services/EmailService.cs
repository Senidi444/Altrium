using System.Net;
using System.Net.Mail;
using AltriumRecruitmentSystem.Models;
using Microsoft.Extensions.Options;

namespace AltriumRecruitmentSystem.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(
            string recipientEmail,
            string subject,
            string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    return false;
                }

                using var smtpClient = new SmtpClient(
                    _settings.SmtpHost,
                    _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(
                        _settings.Username,
                        _settings.Password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(
                        _settings.SenderEmail,
                        _settings.SenderName),

                    Subject = subject,
                    Body = message,
                    IsBodyHtml = false
                };

                mailMessage.To.Add(recipientEmail);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation(
                    "Email sent successfully to {Email}",
                    recipientEmail);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Email}",
                    recipientEmail);

                return false;
            }
        }
    }
}
