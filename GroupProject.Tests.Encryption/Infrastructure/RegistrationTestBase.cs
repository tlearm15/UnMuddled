using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Infrastructure;

/// <summary>
/// TQ-
/// Shared setup for registration tests: reading the form's security token and submitting
/// the form over HTTP. Each test that uses this class gets its own app and test database.
/// </summary>
public abstract class RegistrationTestBase
{
    protected AppTestFactory Factory { get; private set; } = null!;
    [SetUp]
    public void SetUp() => Factory = new AppTestFactory();
    [TearDown]
    public void TearDown() => Factory.Dispose();
    /// <summary>
    /// A client that stops at redirects so tests can check where the form redirected to.
    /// </summary>
    private HttpClient CreateNoRedirectClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    /// <summary>Loads the registration page and reads its security token.</summary>
    private async Task<(string Token, string? Cookie)> GetAntiForgeryTokenAsync(string path)
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        var cookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? string.Join("; ", values.Select(v => v.Split(';')[0]))
            : null;
        return (match.Success ? match.Groups[1].Value : "", cookie);
    }
    /// <summary>Fills in and submits the registration form over HTTP.</summary>
    protected async Task<HttpResponseMessage> RegisterViaHttpAsync(
        string email,
        string firstName,
        string lastName,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? city = null,
        string? postCode = null)
    {
        var (token, cookie) = await GetAntiForgeryTokenAsync( "/Account/Register" );
        Assert.That( token, Is.Not.Empty, "register page must supply an antiforgery token" );

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FirstName"] = firstName,
            ["LastName"] = lastName,
            ["Email"] = email,
            ["Password"] = "Password123!",
            ["ConfirmPassword"] = "Password123!"
        };
        
        if( addressLine1 is not null )
            form["AddressLine1"] = addressLine1;
        if( addressLine2 is not null )
            form["AddressLine2"] = addressLine2;
        if( city is not null )
            form["City"] = city;
        if( postCode is not null )
            form["PostCode"] = postCode;

        var request = new HttpRequestMessage( HttpMethod.Post, "/Account/Register" )
        {
            Content = new FormUrlEncodedContent(form)
        };
        if( cookie is not null ) 
            request.Headers.Add( "Cookie", cookie );
        
        return await CreateNoRedirectClient().SendAsync( request );
    }
}
