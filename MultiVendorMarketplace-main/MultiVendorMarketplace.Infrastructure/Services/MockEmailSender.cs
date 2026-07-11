using Microsoft.Extensions.Logging;
using MultiVendorMarketplace.Application.Interfaces;

namespace MultiVendorMarketplace.Infrastructure.Services
{
    public class MockEmailSender : IEmailSender
    {
        private readonly ILogger<MockEmailSender> _logger;
        private readonly string _emailDirectory;

        public MockEmailSender(ILogger<MockEmailSender> logger)
        {
            _logger = logger;
            // Place email files under a workspace subfolder
            _emailDirectory = Path.Combine(Directory.GetCurrentDirectory(), "emails");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                if (!Directory.Exists(_emailDirectory))
                {
                    Directory.CreateDirectory(_emailDirectory);
                }

                string sanitizedEmail = email.Replace("@", "_at_").Replace(".", "_");
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssff");
                string fileName = $"{timestamp}-{sanitizedEmail}.html";
                string filePath = Path.Combine(_emailDirectory, fileName);

                string fileContent = $@"<!--
Recipient: {email}
Subject: {subject}
Timestamp: {DateTime.UtcNow}
-->
<!DOCTYPE html>
<html>
<head>
    <title>{subject}</title>
</head>
<body style=""font-family: Arial, sans-serif; padding: 20px; line-height: 1.6;"">
    <div style=""max-width: 600px; margin: 0 auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
        <h2 style=""color: #4f46e5; border-bottom: 2px solid #f3f4f6; padding-bottom: 10px;"">Multi-Vendor Marketplace</h2>
        {htmlMessage}
        <hr style=""border: 0; border-top: 1px solid #f3f4f6; margin-top: 20px;"" />
        <p style=""font-size: 12px; color: #9ca3af; text-align: center;"">This is an automated sandbox notification.</p>
    </div>
</body>
</html>";

                await File.WriteAllTextAsync(filePath, fileContent);
                _logger.LogInformation("Mock Email sent to {Recipient}. Saved to {Path}", email, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write mock email file.");
            }
        }
    }
}
