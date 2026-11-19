using GroupProject.Models;
using GroupProject.Tests.Encryption.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Integration;
/// <summary>
/// TQ-
/// Older accounts saved addresses as plain text before encryption was added (Issue #83).
/// These tests check that those accounts still load and that old and new addresses work
/// side by side.
/// </summary>
[TestFixture]
public class LegacyDataCompatibilityTests
{
    [Test]
    public void LegacyPlaintextRows_LoadAlongsideEncryptedRows()
    {
        using var db = new SqliteTestDb();
        // New account: the address is scrambled when saved.
        db.Context.Users.Add(new User
        {
            UserName = "modern@test.com",
            Email = "modern@test.com",
            AddressLine1 = "50 Modern Avenue, Fife"
        });
        // Old account: create a user normally, then put plain text straight into the
        // database to mimic a record saved before encryption existed.
        db.Context.Users.Add(new User
        {
            UserName = "legacy@test.com",
            Email = "legacy@test.com",
            AddressLine1 = "placeholder"
        });
        db.Context.SaveChanges();

        db.Context.Database.ExecuteSqlRaw(
            "UPDATE AspNetUsers SET AddressLine1='12 Legacy Street', City='Dundee', PostCode='DD1 1BB' WHERE UserName='legacy@test.com'");
        db.Context.ChangeTracker.Clear();

        using var fresh = db.CreateFreshContext();
        var modern = fresh.Users.First(u => u.UserName == "modern@test.com");
        var legacy = fresh.Users.First(u => u.UserName == "legacy@test.com");

        Assert.Multiple(() =>
        {
            Assert.That(modern.AddressLine1, Is.EqualTo("50 Modern Avenue, Fife"),
                "encrypted row must decrypt");
            Assert.That(legacy.AddressLine1, Is.EqualTo("12 Legacy Street"),
                "legacy plaintext must pass through unchanged");
            Assert.That(legacy.City, Is.EqualTo("Dundee"));
            Assert.That(legacy.PostCode, Is.EqualTo("DD1 1BB"));
        });
    }

    [Test]
    public void SavingLegacyRowBack_EncryptsExactlyOnce()
    {
        using var db = new SqliteTestDb();

        db.Context.Users.Add(new User
        {
            UserName = "legacy2@test.com",
            Email = "legacy2@test.com",
            AddressLine1 = "placeholder"
        });
        db.Context.SaveChanges();
        db.Context.Database.ExecuteSqlRaw(
            "UPDATE AspNetUsers SET AddressLine1='7 Old Close, Perth' WHERE UserName='legacy2@test.com'");
        db.Context.ChangeTracker.Clear();
        // Re-saving the same plain text would not count as a change, so this test
        // edits the address as a user would:
        using (var updateCtx = db.CreateFreshContext())
        {
            var user = updateCtx.Users.First(u => u.UserName == "legacy2@test.com");
            user.AddressLine1 = "7 Old Close, Perth, PH1"; // profile edit
            updateCtx.SaveChanges();
        }

        var stored = db.ExecuteScalar("SELECT AddressLine1 FROM AspNetUsers WHERE UserName='legacy2@test.com'") as string;
        using var readCtx = db.CreateFreshContext();
        var reloaded = readCtx.Users.First(u => u.UserName == "legacy2@test.com");

        Assert.Multiple(() =>
        {
            Assert.That(reloaded.AddressLine1, Is.EqualTo("7 Old Close, Perth, PH1"),
                "value must read back correctly after first-ever encryption");
            Assert.That(stored, Is.Not.EqualTo("7 Old Close, Perth, PH1"),
                "after save the row must now be encrypted (not plaintext)");
        });
    }
}
