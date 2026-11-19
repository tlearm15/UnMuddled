using GroupProject.Models;
using GroupProject.Tests.Encryption.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Integration;
/// <summary>
/// TQ-
/// Tests that saved addresses survive the app stopping and starting again.
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
        // Keep the test database open outside the app so it survives a simulated restart
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        User? reloaded;
        // ---- First run: create a user, then close the app ----
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
        } // closing the factory is like stopping the app
        // ---- Second run: start the app again over the same database ----
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
    /// Checks that after a restart the saved address is still scrambled in the database,
    /// so someone with only a copy of the database cannot read it. The wrong-key case is
    /// covered by the unit tests; see BUGS.md for a note about changing keys while the app
    /// is running.
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
            var stored = secondRun.ExecuteScalar(
                "SELECT AddressLine1 FROM AspNetUsers WHERE Email='ciphercheck@test.com'") as string;

            Assert.That(stored, Is.Not.Null.And.Not.EqualTo(Address),
                "after an app restart the address must still be ciphertext on disk, never plaintext");
        }
    }
}
