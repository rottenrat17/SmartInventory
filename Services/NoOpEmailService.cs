using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SmartInventoryManagement.Services
{
    /// <summary>
    /// A no-operation email service that logs but doesn't actually send emails.
    /// This ensures the application can function without email capabilities.
    /// </summary>
    public class NoOpEmailService : IEmailService
    {
        private readonly ILogger<NoOpEmailService> _logger;

        public NoOpEmailService(ILogger<NoOpEmailService> logger)
        {
            _logger = logger;
            _logger.LogInformation("NoOpEmailService initialized - all emails will be logged but not sent");
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            _logger.LogInformation("Email NOT sent (NoOp): To: {Email}, Subject: {Subject}", email, subject);
            return Task.CompletedTask;
        }

        public Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            _logger.LogInformation("Confirmation email NOT sent (NoOp): To: {Email}", email);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string resetLink)
        {
            _logger.LogInformation("Password reset email NOT sent (NoOp): To: {Email}", email);
            return Task.CompletedTask;
        }
    }
} 