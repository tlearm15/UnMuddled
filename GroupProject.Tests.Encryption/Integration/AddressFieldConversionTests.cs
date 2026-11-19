using GroupProject.Models;
using GroupProject.Tests.Encryption.Infrastructure;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Integration;
/// <summary>
/// TQ-
/// Tests that addresses are scrambled when saved and readable again when loaded. Empty
/// addresses and the encryption code itself are covered by the unit tests, so these tests
/// only check that the app is wired up correctly.
/// </summary>
[TestFixture]
public class AddressFieldConversionTests
{
    [Test]
    public void SavingUser_StoresCiphertext_AndFreshContext_DecryptsToOriginal()
    {
        using var db = new SqliteTestDb();
        const string address = "10 Downing Street", city = "London", postCode = "SW1A 2AA";

        db.Context.Users.Add(new User
        {
            UserName = "conv1@test.com",
            Email = "conv1@test.com",
            AddressLine1 = address,
            AddressLine2 = "Flat 3",
            City = city,
            PostCode = postCode
        });
        
        db.Context.SaveChanges();
        // Read the database directly to check what was actually stored.
        var raw1 = db.ExecuteScalar("SELECT AddressLine1 FROM AspNetUsers WHERE UserName='conv1@test.com'") as string;
        var raw2 = db.ExecuteScalar("SELECT City FROM AspNetUsers WHERE UserName='conv1@test.com'") as string;
        var raw3 = db.ExecuteScalar("SELECT PostCode FROM AspNetUsers WHERE UserName='conv1@test.com'") as string;
        // Loading in a fresh context checks that the stored value can be read back.
        using var fresh = db.CreateFreshContext();
        var user = fresh.Users.First(u => u.UserName == "conv1@test.com");

        Assert.Multiple(() =>
        {
            Assert.That(raw1, Is.Not.Null.And.Not.Empty.And.Not.EqualTo(address), "AddressLine1 must be encrypted on disk");
            Assert.That(raw2, Is.Not.Null.And.Not.EqualTo(city), "City must be encrypted on disk");
            Assert.That(raw3, Is.Not.Null.And.Not.EqualTo(postCode), "PostCode must be encrypted on disk");
            Assert.That(user.AddressLine1, Is.EqualTo(address));
            Assert.That(user.AddressLine2, Is.EqualTo("Flat 3"));
            Assert.That(user.City, Is.EqualTo(city));
            Assert.That(user.PostCode, Is.EqualTo(postCode));
        });
    }

    [Test]
    public void UpdatingAddress_ReencryptsWithoutDoubleEncryption()
    {
        using var db = new SqliteTestDb();

        db.Context.Users.Add(new User
        {
            UserName = "conv5@test.com",
            Email = "conv5@test.com",
            AddressLine1 = "First Address, Leeds"
        });
        db.Context.SaveChanges();
        // Update the address as the app would, in a fresh context
        using (var updateCtx = db.CreateFreshContext())
        {
            var user = updateCtx.Users.First(u => u.UserName == "conv5@test.com");
            user.AddressLine1 = "Updated Address, Leeds";
            updateCtx.SaveChanges();
        }
        
        using var readCtx = db.CreateFreshContext();
        var reloaded = readCtx.Users.First(u => u.UserName == "conv5@test.com");
        // If it were scrambled twice, the address would come back garbled.
        Assert.That(reloaded.AddressLine1, Is.EqualTo("Updated Address, Leeds"));
    }
}
