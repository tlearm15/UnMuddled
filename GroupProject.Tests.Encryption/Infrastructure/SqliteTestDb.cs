using GroupProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Tests.Encryption.Infrastructure;

/// <summary>
/// A completely fresh in-memory SQLite database per instance (i.e. per test).
/// The open SqliteConnection keeps the :memory: database alive; disposing this
/// object discards the database entirely, so no state leaks between tests.
/// </summary>
public sealed class SqliteTestDb : IDisposable
{
    public SqliteConnection Connection { get; }
    public UnMuddledDbContext Context { get; }

    public SqliteTestDb()
    {
        Connection = new SqliteConnection("Data Source=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<UnMuddledDbContext>()
            .UseSqlite(Connection)
            .Options;

        Context = new UnMuddledDbContext(
            options,
            new FakeEncryptionKeyProvider(),
            new GroupProject.Services.EncryptionService());

        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// A second, independent DbContext over the SAME database - used to prove data
    /// survives a fresh context (simulating an app restart re-opening the DB).
    /// </summary>
    public UnMuddledDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<UnMuddledDbContext>()
            .UseSqlite(Connection)
            .Options;
        return new UnMuddledDbContext(
            options,
            new FakeEncryptionKeyProvider(),
            new GroupProject.Services.EncryptionService());
    }

    /// <summary>Runs a scalar SQL query directly on the connection, bypassing EF and its converters,
    /// so tests can assert on what is physically stored in the database.
    /// SQL NULL is normalised to a real null reference.</summary>
    public object? ExecuteScalar(string sql)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        var result = cmd.ExecuteScalar();
        return result == DBNull.Value ? null : result;
    }

    public void Dispose()
    {
        Context.Dispose();
        Connection.Dispose();
    }
}
