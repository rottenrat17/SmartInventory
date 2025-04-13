using Microsoft.Extensions.Configuration;
using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartInventoryManagement.Services
{
    public class MailerSendEmailService : IEmailService
    {
        private readonly ILogger<MailerSendEmailService> _logger;
        private readonly IConfiguration? _configuration;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public MailerSendEmailService(IConfiguration configuration, ILogger<MailerSendEmailService> logger)
        {
            try
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                
                _host = _configuration["MailerSend:Host"] ?? "smtp.mailersend.net";
                _port = int.Parse(_configuration["MailerSend:Port"] ?? "587");
                _username = _configuration["MailerSend:Username"] ?? "default_username";
                _password = _configuration["MailerSend:Password"] ?? "default_password";
                _fromEmail = _configuration["MailerSend:FromEmail"] ?? _username;
                _fromName = _configuration["MailerSend:FromName"] ?? "Smart Inventory System";

                _logger.LogInformation("MailerSendEmailService initialized with: Host={Host}, Port={Port}, Username={Username}, FromEmail={FromEmail}",
                    _host, _port, _username, _fromEmail);
            }
            catch (Exception ex)
            {
                // Ensure service still initializes even if configuration fails
                logger?.LogError(ex, "Error initializing MailerSendEmailService, using defaults");
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _host = "localhost";
                _port = 25;
                _username = "default_username";
                _password = "default_password";
                _fromEmail = "noreply@example.com";
                _fromName = "Smart Inventory System";
            }
        }

        // Constructor for testing
        protected MailerSendEmailService(ILogger<MailerSendEmailService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = null;
            _fromEmail = "test@example.com";
            _fromName = "Test Sender";
            _host = "localhost";
            _port = 25;
            _username = "testuser";
            _password = "testpass";
        }

        public virtual async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                _logger.LogInformation("Attempting to send email to {Email} with subject: {Subject}", email, subject);
                
                using var client = new SmtpClient(_host, _port);
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(_username, _password);
                client.EnableSsl = true;
                
                _logger.LogDebug("SMTP client configured: UseDefaultCredentials={UseDefaultCredentials}, EnableSsl={EnableSsl}", 
                    client.UseDefaultCredentials, client.EnableSsl);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                _logger.LogDebug("Mail message created: From={From}, To={To}, Subject={Subject}, IsBodyHtml={IsBodyHtml}", 
                    mailMessage.From, email, subject, mailMessage.IsBodyHtml);
                
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}: {ErrorMessage}", email, ex.Message);
                
                // Instead of throwing exception, log it but return so caller can continue
                // This helps with password reset where we want to continue even if email fails
                if (subject.Contains("Reset your password"))
                {
                    _logger.LogWarning("Password reset email failed, but process will continue");
                    return;
                }
                
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }

        public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            try
            {
                _logger.LogInformation("Email confirmation is disabled, but still logging the request for: {Email}", email);
                
                // Don't actually try to send the email - since we're disabling email verification
                // This is safer than actually trying to send an email that might fail
                _logger.LogInformation("Skipping actual email sending as confirmations are disabled");
                
                // Return immediately without attempting to send
                return;
            }
            catch (Exception ex)
            {
                // Log but don't throw - we don't want email failures to block registration
                _logger.LogError(ex, "Error in email confirmation process for {Email}, but continuing", email);
            }
        }

        public async Task SendPasswordResetAsync(string email, string resetLink)
        {
            _logger.LogInformation("Sending password reset to {Email} with link: {Link}", email, resetLink);
            
            var subject = "Reset your password";
            var htmlMessage = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ padding: 20px; max-width: 600px; margin: 0 auto; }}
                        .header {{ background-color: #2196F3; padding: 10px; color: white; text-align: center; }}
                        .content {{ padding: 20px; border: 1px solid #ddd; }}
                        .button {{ background-color: #2196F3; color: white; padding: 10px 20px; text-decoration: none; display: inline-block; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Password Reset</h2>
                        </div>
                        <div class='content'>
                            <h3>Hello!</h3>
                            <p>You recently requested to reset your password. Please click the button below to set a new password:</p>
                            <p><a href='{resetLink}' class='button'>Reset Password</a></p>
                            <p>If the button doesn't work, you can also copy and paste the following link into your browser:</p>
                            <p>{resetLink}</p>
                            <p>If you did not request a password reset, please ignore this email.</p>
                            <p>Thank you,<br>Smart Inventory Team</p>
                        </div>
                    </div>
                </body>
                </html>";
            
            await SendEmailAsync(email, subject, htmlMessage);
        }
    }
} 