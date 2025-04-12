using Microsoft.Extensions.Configuration;
using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartInventoryManagement.Services
{
    public class MailerSendEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        protected readonly ILogger<MailerSendEmailService> _logger;
        protected string _fromEmail;
        protected string _fromName;
        protected string _host;
        protected int _port;
        protected string _username;
        protected string _password;

        public MailerSendEmailService(IConfiguration configuration, ILogger<MailerSendEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            _fromEmail = _configuration["MailerSend:FromEmail"] ?? throw new ArgumentNullException("MailerSend:FromEmail is not configured");
            _fromName = _configuration["MailerSend:FromName"] ?? throw new ArgumentNullException("MailerSend:FromName is not configured");
            _host = _configuration["MailerSend:Host"] ?? throw new ArgumentNullException("MailerSend:Host is not configured");
            _port = int.Parse(_configuration["MailerSend:Port"] ?? "587");
            _username = _configuration["MailerSend:Username"] ?? throw new ArgumentNullException("MailerSend:Username is not configured");
            _password = _configuration["MailerSend:Password"] ?? throw new ArgumentNullException("MailerSend:Password is not configured");
            
            _logger.LogInformation("MailerSendEmailService initialized with: Host={Host}, Port={Port}, Username={Username}, FromEmail={FromEmail}", 
                _host, _port, _username, _fromEmail);
        }

        // Constructor for testing
        protected MailerSendEmailService(ILogger<MailerSendEmailService> logger)
        {
            _logger = logger;
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
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }

        public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            _logger.LogInformation("Sending email confirmation to {Email} with link: {Link}", email, confirmationLink);
            
            var subject = "Confirm your email";
            var htmlMessage = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ padding: 20px; max-width: 600px; margin: 0 auto; }}
                        .header {{ background-color: #4CAF50; padding: 10px; color: white; text-align: center; }}
                        .content {{ padding: 20px; border: 1px solid #ddd; }}
                        .button {{ background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; display: inline-block; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Email Confirmation</h2>
                        </div>
                        <div class='content'>
                            <h3>Hello!</h3>
                            <p>Thank you for registering with Smart Inventory System. Please confirm your email address by clicking the button below:</p>
                            <p><a href='{confirmationLink}' class='button'>Confirm Email</a></p>
                            <p>If the button doesn't work, you can also copy and paste the following link into your browser:</p>
                            <p>{confirmationLink}</p>
                            <p>Thank you,<br>Smart Inventory Team</p>
                        </div>
                    </div>
                </body>
                </html>";
            
            await SendEmailAsync(email, subject, htmlMessage);
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