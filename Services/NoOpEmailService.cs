using Microsoft.Extensions.Logging;
using System;
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
        private static readonly ILogger<NoOpEmailService> _fallbackLogger;

        // Static constructor ensures we always have a logger to use
        static NoOpEmailService()
        {
            var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
            _fallbackLogger = loggerFactory.CreateLogger<NoOpEmailService>();
        }

        public NoOpEmailService(ILogger<NoOpEmailService> logger = null)
        {
            _logger = logger ?? _fallbackLogger;
            _logger.LogInformation("NoOpEmailService initialized - all emails will be logged but not sent");
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                _logger.LogInformation("Email NOT sent (NoOp): To: {Email}, Subject: {Subject}", 
                    email ?? "null", subject ?? "null");
            }
            catch
            {
                // Swallow all exceptions - this service must never fail
            }
            return Task.CompletedTask;
        }

        public Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            try
            {
                _logger.LogInformation("Confirmation email NOT sent (NoOp): To: {Email}", 
                    email ?? "null");
            }
            catch
            {
                // Swallow all exceptions - this service must never fail
            }
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string resetLink)
        {
            try
            {
                _logger.LogInformation("Password reset email NOT sent (NoOp): To: {Email}", 
                    email ?? "null");
            }
            catch
            {
                // Swallow all exceptions - this service must never fail
            }
            return Task.CompletedTask;
        }
    }
} 