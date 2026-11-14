using GroupProject.Models;
using GroupProject.Tests.Encryption.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Integration;

/// <summary>
/// Pre-migration compatibility (Issue #83): rows written before the AddressEncryption
/// feature hold PLAINTEXT addresses. They must load without error (passthrough) and
/// coexist with encrypted rows written after the feature.
/// </summary>
[TestFixture]
public class LegacyDataCompatibilityTests
{
    [Test]
    public void LegacyPlaintextRow_LoadsUnchanged_WithoutError()
    {
        using var db = new SqliteTestDb();

        // Create a normal user first (satisfies identity schema), then overwrite the
        // address columns with plaintext, simulating a pre-migration row.
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
        var user = fresh.Users.First(u => u.UserName == "legacy@test.com");

        Assert.Multiple(() =>
        {
            Assert.That(user.AddressLine1, Is.EqualTo("12 Legacy Street"),
                "legacy plaintext must pass through unchanged");
            Assert.That(user.City, Is.EqualTo("Dundee"));
            Assert.That(user.PostCode, Is.EqualTo("DD1 1BB"));
        });
    }

    [Test]
    public void MixedEncryptedAndLegacyRows_LoadTogether()
    {
        using var db = new SqliteTestDb();

        // Encrypted row (written post-feature, through the converter)
        db.Context.Users.Add(new User
        {
            UserName = "modern@test.com",
            Email = "modern@test.com",
            AddressLine1 = "50 Modern Avenue, Fife"
        });
        // Legacy row, immediately corrupted down to plaintext
        db.Context.Users.Add(new User
        {
            UserName = "legacy@test.com",
            Email = "legacy@test.com",
            AddressLine1 = "placeholder"
        });
        db.Context.SaveChanges();
        db.Context.Database.ExecuteSqlRaw(
            "UPDATE AspNetUsers SET AddressLine1='51 Legacy Lane, Fife' WHERE UserName='legacy@test.com'");
        db.Context.ChangeTracker.Clear();

        using var fresh = db.CreateFreshContext();
        var modern = fresh.Users.First(u => u.UserName == "modern@test.com");
        var legacy = fresh.Users.First(u => u.UserName == "legacy@test.com");

        Assert.Multiple(() =>
        {
            Assert.That(modern.AddressLine1, Is.EqualTo("50 Modern Avenue, Fife"),
                "encrypted row must decrypt");
            Assert.That(legacy.AddressLine1, Is.EqualTo("51 Legacy Lane, Fife"),
                "legacy plaintext row must pass through and coexist with encrypted rows");
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

        // Saving the UNCHANGED plaintext value would be a no-op for EF's change tracker
        // (no UPDATE, no encryption) - the user actually edits their address:
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
