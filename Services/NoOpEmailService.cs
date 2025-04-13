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
        public NoOpEmailService(ILogger<NoOpEmailService>? logger = null)
        {
            // Empty constructor - no dependencies
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            // Do nothing
            return Task.CompletedTask;
        }

        public Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            // Do nothing
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string resetLink)
        {
            // Do nothing
            return Task.CompletedTask;
        }
    }
} 