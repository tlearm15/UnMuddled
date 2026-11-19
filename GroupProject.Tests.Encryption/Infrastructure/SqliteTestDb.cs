using GroupProject.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Tests.Encryption.Infrastructure;
/// <summary>
/// TQ-
/// A brand-new empty test database for each test. The connection stays open while the test
/// runs; disposing this object throws the database away, so tests do not affect each other.
/// </summary>
public sealed class SqliteTestDb : IDisposable
{
    private SqliteConnection Connection { get; }
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
    /// Opens a second connection to the same database, like restarting the app and
    /// reopening its data.
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
    /// <summary>
    /// Runs a query against the test database and returns one value, so tests can check
    /// what was actually stored. An empty database value is returned as null.
    /// </summary>
    public object? ExecuteScalar(string sql)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        var result = cmd.ExecuteScalar();
        return result == DBNull.Value ? null : result;
    }
    /// <summary>
    /// Closes the database after the test.
    /// </summary>
    public void Dispose()
    {
        Context.Dispose();
        Connection.Dispose();
    }
}
