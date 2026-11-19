using System.Net;
using GroupProject.Models;
using GroupProject.Tests.Encryption.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Integration;
/// <summary>
/// TQ-
/// Tests the full registration form: submitting it creates a user, stores the address
/// scrambled, and reads it back correctly. The shared form-submission helper lives in
/// <see cref="RegistrationTestBase"/>.
/// </summary>
[TestFixture]
public class UserRegistrationIntegrationTests : RegistrationTestBase
{
    [Test]
    public async Task RegisterViaHttp_CreatesUserWithEncryptedAddress()
    {
        const string 
            fakeEmail = "dudebro@test.com",
            fakeFirstName = "John",
            fakeLastName = "Doe",
            fakeAddressLine1 = "Apartment 1",
            fakeAddressLine2 = "9 Drewery Lane",
            fakeCity = "York",
            fakePostCode = "YO1 7HB";
        
        var response = await RegisterViaHttpAsync( 
            fakeEmail,
            fakeFirstName,
            fakeLastName,
            fakeAddressLine1,
            fakeAddressLine2,
            fakeCity,
            fakePostCode );

        Assert.Multiple(() =>
        {
            Assert.That( response.StatusCode, Is.EqualTo( HttpStatusCode.Redirect ),
                "successful registration redirects to the login page");
            Assert.That( response.Headers.Location?.ToString(), Does.Contain( "/Account/Login" ) );
        });
        // Check the saved user through the app's own user manager
        await using var scope = Factory.CreateTestScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync( fakeEmail );
        Assert.That( user, Is.Not.Null, "registered user must exist in the database" );
        Assert.Multiple(() =>
        {
            Assert.That( user!.AddressLine1, Is.EqualTo( fakeAddressLine1 ),
                "address must decrypt to the original value through the app" );
            Assert.That( user!.AddressLine2, Is.EqualTo( fakeAddressLine2 ) );
            Assert.That( user!.City, Is.EqualTo( fakeCity ) );
            Assert.That( user!.PostCode, Is.EqualTo( fakePostCode ) );
        } );
    }

    [Test]
    public async Task RegisteredAddress_IsEncryptedOnDisk()
    {
        const string 
            fakeEmail = "guyfella@test.com",
            fakeFirstName = "Jim",
            fakeLastName = "Bob",
            fakeAddressLine1 = "Apartment 2",
            fakeAddressLine2 = "12 Sunshine Boulevard",
            fakeCity = "Brighton",
            fakePostCode = "BO1 6HB";
        
        await RegisterViaHttpAsync(
            fakeEmail, fakeFirstName, fakeLastName,
            fakeAddressLine1, fakeAddressLine2, fakeCity, fakePostCode );
        // Read the database directly to check what was actually stored.
        Assert.Multiple(() =>
        {
            Assert.That( Factory.ExecuteScalar( $"SELECT AddressLine1 FROM AspNetUsers WHERE Email='{fakeEmail}'" ) as string,
                Is.Not.Null.And.Not.EqualTo( fakeAddressLine1 ), "AddressLine1 must be ciphertext on disk" );
            Assert.That( Factory.ExecuteScalar( $"SELECT AddressLine2 FROM AspNetUsers WHERE Email='{fakeEmail}'" ) as string,
                Is.Not.Null.And.Not.EqualTo( fakeAddressLine2 ), "AddressLine2 must be ciphertext on disk" );
            Assert.That( Factory.ExecuteScalar( $"SELECT City FROM AspNetUsers WHERE Email='{fakeEmail}'" ) as string,
                Is.Not.Null.And.Not.EqualTo( fakeCity ), "City must be ciphertext on disk" );
            Assert.That( Factory.ExecuteScalar( $"SELECT PostCode FROM AspNetUsers WHERE Email='{fakeEmail}'" ) as string,
                Is.Not.Null.And.Not.EqualTo( fakePostCode ), "PostCode must be ciphertext on disk" );
        } );
    }
}
