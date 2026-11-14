using GroupProject.Models;
using GroupProject.Tests.Encryption.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Integration;

/// <summary>
/// Data persistence: save an encrypted user, "stop the app" (dispose the factory /
/// drop all contexts), "restart" (a brand-new app + context over the same database),
/// reload the user and verify the data survived intact and decrypts correctly.
/// </summary>
[TestFixture]
public class AddressPersistenceIntegrationTests
{
    private const string Address = "99 Persistence Road, Aberdeen";
    private const string City = "Aberdeen";
    private const string PostCode = "AB10 1XG";

    [Test]
    public void UserData_SurvivesApplicationRestart()
    {
        // Open the database OUTSIDE the app so it can outlive the "process"
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        User? reloaded;

        // ---- First "process": create user, then stop the app ----
        using (var firstRun = new AppTestFactory(connection))
        {
            using var scope = firstRun.CreateTestScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var created = userManager.CreateAsync(new User
            {
                UserName = "persist@test.com",
                Email = "persist@test.com",
                EmailConfirmed = true,
                FirstName = "Per",
                LastName = "Sister",
                AddressLine1 = Address,
                AddressLine2 = "Suite 5",
                City = City,
                PostCode = PostCode
            }, "Password123!").GetAwaiter().GetResult();
            Assert.That(created.Succeeded, "user creation must succeed");
        } // firstRun.Dispose() == app stopped; all contexts dropped

        // ---- Second "process": restart over the same database ----
        using (var secondRun = new AppTestFactory(connection))
        {
            using var scope = secondRun.CreateTestScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            reloaded = userManager.FindByEmailAsync("persist@test.com").GetAwaiter().GetResult();
        }

        Assert.Multiple(() =>
        {
            Assert.That(reloaded, Is.Not.Null, "user must survive the restart");
            Assert.That(reloaded!.AddressLine1, Is.EqualTo(Address));
            Assert.That(reloaded.AddressLine2, Is.EqualTo("Suite 5"));
            Assert.That(reloaded.City, Is.EqualTo(City));
            Assert.That(reloaded.PostCode, Is.EqualTo(PostCode));
        });
    }

    /// <summary>
    /// Second "process" runs against the same database; the stored address must be
    /// CIPHERTEXT on disk - i.e. anyone with only the database (no key) cannot read
    /// the address. (Key-rotation behaviour is covered by the unit-level wrong-key tests;
    /// see BUGS.md for the EF model-cache caveat around swapping key providers at runtime.)
    /// </summary>
    [Test]
    public void RestartedApp_StoredAddress_IsNeverPlaintextOnDisk()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using (var firstRun = new AppTestFactory(connection))
        {
            using var scope = firstRun.CreateTestScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var created = userManager.CreateAsync(new User
            {
                UserName = "ciphercheck@test.com",
                Email = "ciphercheck@test.com",
                EmailConfirmed = true,
                AddressLine1 = Address
            }, "Password123!").GetAwaiter().GetResult();
            Assert.That(created.Succeeded);
        }

        using (var secondRun = new AppTestFactory(connection))
        {
            using var scope = secondRun.CreateTestScope();
            var db = scope.ServiceProvider.GetRequiredService<UnMuddledDbContext>();
            var dbConnection = db.Database.GetDbConnection();
            dbConnection.Open();
            using var cmd = dbConnection.CreateCommand();
            cmd.CommandText = "SELECT AddressLine1 FROM AspNetUsers WHERE Email='ciphercheck@test.com'";
            var stored = cmd.ExecuteScalar() as string;

            Assert.That(stored, Is.Not.Null.And.Not.EqualTo(Address),
                "after an app restart the address must still be ciphertext on disk, never plaintext");
        }
    }
}
