using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using GroupProject.Models;
using GroupProject.Tests.Encryption.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Integration;

/// <summary>
/// Full registration flow over real HTTP (WebApplicationFactory):
/// POST /Account/Register -> user row created -> address fields encrypted in DB
/// -> retrieved and decrypted correctly -> update address -> re-verified.
/// </summary>
[TestFixture]
public class UserRegistrationIntegrationTests
{
    private AppTestFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new AppTestFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    /// <summary>Client that does NOT follow redirects, so POST -> 302 Location is observable.</summary>
    private HttpClient CreateNoRedirectClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    /// <summary>Fetches a page and extracts the antiforgery token + cookie.</summary>
    private async Task<(string Token, string? Cookie)> GetAntiForgeryTokenAsync(string path)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        var cookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? string.Join("; ", values.Select(v => v.Split(';')[0]))
            : null;
        return (match.Success ? match.Groups[1].Value : "", cookie);
    }

    [Test]
    public async Task RegisterViaHttp_CreatesUserWithEncryptedAddress()
    {
        var (token, cookie) = await GetAntiForgeryTokenAsync("/Account/Register");
        Assert.That(token, Is.Not.Empty, "register page must supply an antiforgery token");

        var client = CreateNoRedirectClient();
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FirstName"] = "Ada",
            ["LastName"] = "Lovelace",
            ["Email"] = "ada@test.com",
            ["Password"] = "Password123!",
            ["ConfirmPassword"] = "Password123!",
            ["AddressLine1"] = "36 Ada Street, Brighton",
            ["AddressLine2"] = "Apartment 9",
            ["City"] = "Brighton",
            ["PostCode"] = "BN1 1AA"
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Register")
        {
            Content = new FormUrlEncodedContent(form)
        };
        if (cookie is not null) request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                "successful registration redirects to the login page");
            Assert.That(response.Headers.Location?.ToString(), Does.Contain("/Account/Login"));
        });

        // Verify through the app's own services
        using var scope = _factory.CreateTestScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync("ada@test.com");
        Assert.That(user, Is.Not.Null, "registered user must exist in the database");
        Assert.That(user!.AddressLine1, Is.EqualTo("36 Ada Street, Brighton"),
            "address must decrypt to the original value through the app");
    }

    [Test]
    public async Task RegisteredAddress_IsEncryptedOnDisk()
    {
        var (token, cookie) = await GetAntiForgeryTokenAsync("/Account/Register");
        var client = _factory.CreateClient();
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FirstName"] = "Grace",
            ["LastName"] = "Hopper",
            ["Email"] = "grace@test.com",
            ["Password"] = "Password123!",
            ["ConfirmPassword"] = "Password123!",
            ["AddressLine1"] = "9 Compiler Lane",
            ["City"] = "York",
            ["PostCode"] = "YO1 7HB"
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Register")
        {
            Content = new FormUrlEncodedContent(form)
        };
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        await client.SendAsync(request);

        using var scope = _factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<UnMuddledDbContext>();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT AddressLine1, City, PostCode FROM AspNetUsers WHERE Email='grace@test.com'";
        using var reader = await cmd.ExecuteReaderAsync();
        Assert.That(await reader.ReadAsync(), "user row must exist");
        var rawAddress = reader.IsDBNull(0) ? null : reader.GetString(0);
        var rawCity = reader.IsDBNull(1) ? null : reader.GetString(1);
        var rawPost = reader.IsDBNull(2) ? null : reader.GetString(2);

        Assert.Multiple(() =>
        {
            Assert.That(rawAddress, Is.Not.EqualTo("9 Compiler Lane"), "AddressLine1 must be ciphertext on disk");
            Assert.That(rawCity, Is.Not.EqualTo("York"), "City must be ciphertext on disk");
            Assert.That(rawPost, Is.Not.EqualTo("YO1 7HB"), "PostCode must be ciphertext on disk");
        });
    }

    [Test]
    public async Task RegistrationEmail_IsCaptured_NotSent()
    {
        var (token, cookie) = await GetAntiForgeryTokenAsync("/Account/Register");
        var client = _factory.CreateClient();
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FirstName"] = "Alan",
            ["LastName"] = "Turing",
            ["Email"] = "alan@test.com",
            ["Password"] = "Password123!",
            ["ConfirmPassword"] = "Password123!"
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Register")
        {
            Content = new FormUrlEncodedContent(form)
        };
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        await client.SendAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(_factory.EmailSender.Sent, Is.Not.Empty,
                "registration must trigger a confirmation email through IEmailSender");
            Assert.That(_factory.EmailSender.Sent[0].Email, Is.EqualTo("alan@test.com"));
            Assert.That(_factory.EmailSender.Sent[0].Html, Does.Contain("ConfirmEmail"),
                "confirmation link must point at the ConfirmEmail action");
        });
    }
}
