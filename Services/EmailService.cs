using Microsoft.AspNetCore.Identity;
using SendGrid;
using SendGrid.Helpers.Mail;
using GroupProject.Models;

namespace GroupProject.Services;

public class EmailService(ILogger<EmailService> logger) : IEmailSender<User>
{
    private readonly ILogger _logger = logger;

    //TL - Handles sending account confirmation links for user registrations
    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        return SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");
    }

    //TL - Handles sending password reset links for user account recovery
    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        return SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");
    }

    //TL - Handles sending 2FA codes or code-based verification messages
    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        return SendEmailAsync(email, "Your password reset code", $"Your reset code is: {resetCode}");
    }

    //TL - Validates the SendGrid API key retrieved from .env 
    public async Task SendEmailAsync(string recipient, string subject, string message)
    {
        var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");

        _logger.LogInformation("Loaded API Key length: {Length}", apiKey?.Length ?? 0);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("SendGridKey is null. Check .env file");
            throw new Exception("Null SendGridKey");
        }

        await Execute(apiKey, subject, message, recipient);
    }

    //TL - Sends Email
    private async Task Execute(string apiKey, string subject, string message, string recipient)
    {
        var options = new SendGridClientOptions
        {
            ApiKey = apiKey
        };

        var client = new SendGridClient(options);
        var email = new SendGridMessage()
        {
            From = new EmailAddress("unmuddled1156@gmail.com", "UnMuddled"),
            Subject = subject,
            PlainTextContent = message,
            HtmlContent = message
        };
        
        email.AddTo(new EmailAddress(recipient));
        email.SetClickTracking(false, false);
        
        var response = await client.SendEmailAsync(email);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email to {Recipient} queued successfully!", recipient);
        }
        else
        {
            _logger.LogError("Failure Email to {Recipient}. Status Code: {StatusCode}", recipient, response.StatusCode);
        }
    }
}