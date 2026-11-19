using System.Data;
using GroupProject.Services;
using GroupProject.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GroupProject.Tests.Encryption.Infrastructure;

/// <summary>
/// TQ-
/// Records emails during tests instead of sending them, so tests can check what was sent
/// (such as confirmation links) without needing an email service.
/// </summary>
public sealed class CapturingEmailSender : IEmailSender<User>
{
    public List<(string Email, string Subject, string Html)> Sent { get; } = [];

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        Sent.Add((email, "Confirmation", confirmationLink));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        Sent.Add((email, "Reset", resetLink));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        Sent.Add((email, "ResetCode", resetCode));
        return Task.CompletedTask;
    }
}
/// <summary>
/// Starts the app for tests with a fresh test database, a fixed encryption key (no
/// ENCRYPTION_KEY needed) and an email sender that records messages instead of sending them.
/// The caller can supply the database connection to simulate an app restart.
/// </summary>
public sealed class AppTestFactory : WebApplicationFactory<Program>
{
    private FakeEncryptionKeyProvider KeyProvider { get; } = new();
    public CapturingEmailSender EmailSender { get; } = new();
    private readonly SqliteConnection? _externalConnection;
    private readonly SqliteConnection? _ownedConnection;
    /// <summary>
    /// TQ-
    /// Creates a test app with its own temporary database.
    /// </summary>
    public AppTestFactory()
    {
        _ownedConnection = new SqliteConnection( "Data Source=:memory:" );
        _ownedConnection.Open();
    }
    /// <summary>
    /// Uses an existing database connection so a second test app can simulate
    /// restarting the app.
    /// </summary>
    public AppTestFactory(SqliteConnection existingConnection)
    {
        _externalConnection = existingConnection;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // The app expects a database connection string when it starts. A valid-looking one
        // is enough here because the tests replace the real database later.
        builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=testdb;Username=test;Password=test");
        builder.ConfigureServices(services =>
        {
            // Keep database logs out of the test output
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
            // Remove the app's PostgreSQL setup so the test database is used instead.
            for( var i = services.Count - 1; i >= 0; i-- )
            {
                var st = services[i].ServiceType;
                var remove =
                    st == typeof(DbContextOptions<UnMuddledDbContext>) ||
                    st == typeof(DbContextOptions) ||
                    (st.IsGenericType
                        && st.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                        && st.GetGenericArguments()[0] == typeof(UnMuddledDbContext));
                
                if( remove )
                    services.RemoveAt(i);
            }

            services.AddDbContext<UnMuddledDbContext>(o => o.UseSqlite(_externalConnection ?? _ownedConnection!));

            services.RemoveAll<IEncryptionKeyProvider>();
            services.AddSingleton<IEncryptionKeyProvider>(KeyProvider);

            services.RemoveAll<IEmailSender<User>>();
            services.AddSingleton<IEmailSender<User>>(EmailSender);
        });
    }
    /// <summary>Gets access to the app's services inside a test.</summary>
    public AsyncServiceScope CreateTestScope() => Services.CreateAsyncScope();
    /// <summary>
    /// Runs a query against the test database and returns one value, so tests can check
    /// what was actually stored. An empty database value is returned as null.
    /// </summary>
    public object? ExecuteScalar(string sql)
    {
        using var scope = CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<UnMuddledDbContext>();
        var connection = db.Database.GetDbConnection();
        if( connection.State != ConnectionState.Open )
            connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var result = cmd.ExecuteScalar();
        return result == DBNull.Value ? null : result;
    }

    protected override void Dispose(bool disposing)
    {
        if( disposing )
            _ownedConnection?.Dispose();
        
        base.Dispose(disposing);
    }
}
