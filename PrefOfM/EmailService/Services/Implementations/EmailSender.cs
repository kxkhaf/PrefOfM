using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmailService.Application.Settings;
using EmailService.Exceptions;
using EmailService.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;

namespace EmailService.Services.Implementations
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly EmailTemplates _templates;
        private readonly ILogger<EmailSender> _logger;
        private readonly EmailTemplateLoader _templateLoader;

        public EmailSender(
            IOptions<SmtpSettings> smtpOptions,
            IOptions<EmailTemplates> templateOptions,
            ILogger<EmailSender> logger,
            IWebHostEnvironment env)
        {
            _smtpSettings = smtpOptions?.Value ?? throw new ArgumentNullException(nameof(smtpOptions));
            _templates = templateOptions?.Value ?? throw new ArgumentNullException(nameof(templateOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templateLoader = new EmailTemplateLoader(env);

            ValidateSettings();
        }

        public async Task SendEmailAsync(string email, string subject, string body)
        {
            ValidateEmailParameters(email, subject, body);

            using var client = CreateSmtpClient();
            using var message = CreateMailMessage(email, subject, body);

            try
            {
                _logger.LogInformation("Sending email to {Recipient} with subject '{Subject}'",
                    email, subject);

                await client.SendMailAsync(message);

                _logger.LogInformation("Email successfully sent to {Recipient}", email);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending to {Recipient}. Status: {StatusCode}",
                    email, ex.StatusCode);
                throw new EmailSendException($"SMTP error: {ex.StatusCode}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending to {Recipient}", email);
                throw new EmailSendException("Failed to send email", ex);
            }
        }

        public async Task SendEmailConfirmationAsync(string email, string userId, string token)
        {
            var templateConfig = _templates.EmailConfirmation;
            var confirmUrl = $"{_smtpSettings.BaseUrl}/confirm-email" +
                             $"?userId={WebUtility.UrlEncode(userId)}" +
                             $"&token={WebUtility.UrlEncode(token)}";

            var placeholders = new Dictionary<string, string>
            {
                ["confirmUrl"] = confirmUrl,
                ["email"] = email,
                ["expirationHours"] = templateConfig.ExpirationHours.ToString(),
                ["senderName"] = templateConfig.SenderName
            };

            var body = await _templateLoader.LoadTemplateAsync(
                templateConfig.TemplatePath,
                placeholders);

            await SendEmailAsync(email, templateConfig.Subject, body);
        }

        public async Task SendPasswordResetAsync(string email, string userId, string token)
        {
            var resetUrl = $"{_smtpSettings.BaseUrl}/reset-password" +
                           $"?userId={WebUtility.UrlEncode(userId)}" +
                           $"&token={WebUtility.UrlEncode(token)}";

            var body = await _templateLoader.LoadTemplateAsync(
                _templates.PasswordReset.TemplatePath,
                new Dictionary<string, string>
                {
                    ["resetUrl"] = resetUrl,
                    ["email"] = email
                });

            await SendEmailAsync(email, _templates.PasswordReset.Subject, body);
        }

        public async Task SendEmailChangeConfirmationAsync(string newEmail, string oldEmail, string userId,
            string token)
        {
            var templateConfig = _templates.EmailChangeConfirmation;
            var confirmUrl = $"{_smtpSettings.BaseUrl}/confirm-email-change" +
                             $"?userId={WebUtility.UrlEncode(userId)}" +
                             $"&token={WebUtility.UrlEncode(token)}" +
                             $"&newEmail={WebUtility.UrlEncode(newEmail)}";

            var placeholders = new Dictionary<string, string>
            {
                ["confirmUrl"] = confirmUrl,
                ["newEmail"] = newEmail,
                ["oldEmail"] = oldEmail,
                ["expirationHours"] = templateConfig.ExpirationHours.ToString(),
                ["senderName"] = templateConfig.SenderName
            };
            var body = await _templateLoader.LoadTemplateAsync(
                templateConfig.TemplatePath,
                placeholders);

            await SendEmailAsync(newEmail, templateConfig.Subject, body);
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                EnableSsl = _smtpSettings.UseSsl,
                Credentials = new NetworkCredential(
                    _smtpSettings.User,
                    _smtpSettings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = _smtpSettings.TimeoutMilliseconds
            };
        }

        private MailMessage CreateMailMessage(string to, string subject, string body)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_smtpSettings.From),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                Priority = MailPriority.Normal
            };
            message.To.Add(to);

            return message;
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_smtpSettings.Host))
                throw new ArgumentException("SMTP host is not configured");

            if (string.IsNullOrWhiteSpace(_smtpSettings.From))
                throw new ArgumentException("From address is not configured");

            if (string.IsNullOrWhiteSpace(_smtpSettings.Password))
                throw new ArgumentException("SMTP password is not configured");

            if (string.IsNullOrWhiteSpace(_smtpSettings.BaseUrl))
                throw new ArgumentException("Base URL is not configured");
        }

        private void ValidateEmailParameters(string email, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Recipient email cannot be empty");

            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Email subject cannot be empty");

            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Email body cannot be empty");

            if (!IsValidEmail(email))
                throw new ArgumentException("Invalid email format");
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}