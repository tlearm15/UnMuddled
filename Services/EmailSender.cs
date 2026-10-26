using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace GroupProject.Services;

public class EmailSender(IOptions<SendGridSettings> settings, ILogger<EmailSender> logger) : IEmailSender
{
    private readonly SendGridSettings _settings = settings.Value;
    private readonly SendGridClient _client = new( settings.Value.ApiKey );
    /// <summary>
    /// TQ- Sends an email
    /// </summary>
    /// <param name="email"> The guy's email address who we want to send to</param>
    /// <param name="subject"> Email subject</param>
    /// <param name="htmlMessage"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if( string.IsNullOrWhiteSpace( _settings.ApiKey ) )
        {
            logger.LogError( "SendGrid API Key is missing!" );
            
            throw new InvalidOperationException( "SendGrid API Key is missing — check configuration." );
        }
        
        var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
        var to = new EmailAddress(email);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);
        var response = await _client.SendEmailAsync( msg );

        if( !response.IsSuccessStatusCode)   // SendGrid returns 202 Accepted on success
        {
            var body = await response.Body.ReadAsStringAsync();
            logger.LogError("SendGrid send to {Email} failed ({StatusCode}): {Body}",
                email, (int)response.StatusCode, body);
            
            throw new InvalidOperationException($"Failed to send email to {email} (HTTP {(int)response.StatusCode}).");
        }
        // We succeeded sending the email
        logger.LogInformation( "Email sent to {Email} (subject: {Subject})", email, subject );
    }
}
