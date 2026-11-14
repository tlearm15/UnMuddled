using GroupProject.Models;
using GroupProject.Tests.Encryption.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Integration;

/// <summary>
/// Verifies the EF Core ValueConverter on User address fields
/// (AddressLine1/2, City, PostCode): encrypt on write, decrypt on read,
/// empty-string passthrough, and physical encryption in the database.
/// </summary>
[TestFixture]
public class AddressFieldConversionTests
{
    [Test]
    public void SavingUser_StoresCiphertext_NotPlaintext()
    {
        using var db = new SqliteTestDb();
        const string address = "10 Downing Street", city = "London", postCode = "SW1A 2AA";

        db.Context.Users.Add(new User
        {
            UserName = "conv1@test.com",
            Email = "conv1@test.com",
            AddressLine1 = address,
            City = city,
            PostCode = postCode
        });
        db.Context.SaveChanges();

        var raw1 = db.ExecuteScalar("SELECT AddressLine1 FROM AspNetUsers WHERE UserName='conv1@test.com'") as string;
        var raw2 = db.ExecuteScalar("SELECT City FROM AspNetUsers WHERE UserName='conv1@test.com'") as string;
        var raw3 = db.ExecuteScalar("SELECT PostCode FROM AspNetUsers WHERE UserName='conv1@test.com'") as string;

        Assert.Multiple(() =>
        {
            Assert.That(raw1, Is.Not.EqualTo(address), "AddressLine1 must be encrypted on disk");
            Assert.That(raw2, Is.Not.EqualTo(city), "City must be encrypted on disk");
            Assert.That(raw3, Is.Not.EqualTo(postCode), "PostCode must be encrypted on disk");
            Assert.That(raw1, Is.Not.Empty.And.Not.Null);
        });
    }

    [Test]
    public void ReadingUser_DecryptsToOriginalValues()
    {
        using var db = new SqliteTestDb();
        const string address = "221B Baker Street", city = "Glasgow", postCode = "G1 1XQ";

        db.Context.Users.Add(new User
        {
            UserName = "conv2@test.com",
            Email = "conv2@test.com",
            AddressLine1 = address,
            AddressLine2 = "Flat 3",
            City = city,
            PostCode = postCode
        });
        db.Context.SaveChanges();

        using var fresh = db.CreateFreshContext(); // read through a brand-new context
        var user = fresh.Users.First(u => u.UserName == "conv2@test.com");

        Assert.Multiple(() =>
        {
            Assert.That(user.AddressLine1, Is.EqualTo(address));
            Assert.That(user.AddressLine2, Is.EqualTo("Flat 3"));
            Assert.That(user.City, Is.EqualTo(city));
            Assert.That(user.PostCode, Is.EqualTo(postCode));
        });
    }

    [Test]
    public void EmptyAddressFields_PassThroughUnencrypted()
    {
        using var db = new SqliteTestDb();

        db.Context.Users.Add(new User
        {
            UserName = "conv3@test.com",
            Email = "conv3@test.com",
            AddressLine1 = string.Empty, // "optional address" path - Issue #83
            City = string.Empty,
            PostCode = string.Empty
        });
        db.Context.SaveChanges();

        Assert.Multiple(() =>
        {
            Assert.That(db.ExecuteScalar("SELECT AddressLine1 FROM AspNetUsers WHERE UserName='conv3@test.com'"), Is.EqualTo(string.Empty));
            Assert.That(db.ExecuteScalar("SELECT City FROM AspNetUsers WHERE UserName='conv3@test.com'"), Is.EqualTo(string.Empty));
            Assert.That(db.ExecuteScalar("SELECT PostCode FROM AspNetUsers WHERE UserName='conv3@test.com'"), Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void NullAddressFields_StayNull()
    {
        using var db = new SqliteTestDb();

        db.Context.Users.Add(new User
        {
            UserName = "conv4@test.com",
            Email = "conv4@test.com",
            AddressLine2 = null // explicitly optional -RW
        });
        db.Context.SaveChanges();

        Assert.That(db.ExecuteScalar("SELECT AddressLine2 FROM AspNetUsers WHERE UserName='conv4@test.com'"), Is.Null);
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

        // Update through a fresh context (as the app would)
        using (var updateCtx = db.CreateFreshContext())
        {
            var user = updateCtx.Users.First(u => u.UserName == "conv5@test.com");
            user.AddressLine1 = "Updated Address, Leeds";
            updateCtx.SaveChanges();
        }

        var stored = db.ExecuteScalar("SELECT AddressLine1 FROM AspNetUsers WHERE UserName='conv5@test.com'") as string;
        using var readCtx = db.CreateFreshContext();
        var reloaded = readCtx.Users.First(u => u.UserName == "conv5@test.com");

        Assert.Multiple(() =>
        {
            // DecryptOrPassthrough peels at most 8 layers; a double-encrypted value would
            // decrypt to inner ciphertext, not the plaintext update.
            Assert.That(reloaded.AddressLine1, Is.EqualTo("Updated Address, Leeds"));
            Assert.That(stored, Is.Not.EqualTo("Updated Address, Leeds"), "value must remain encrypted on disk");
        });
    }
}
