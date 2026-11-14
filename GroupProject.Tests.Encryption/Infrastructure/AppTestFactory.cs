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
/// Capturing email sender: records every "sent" email so tests can assert on the
/// actual content (e.g. confirmation links) without any external dependency.
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
/// WebApplicationFactory wired for testing:
/// - fresh in-memory SQLite database (passed in, so callers control lifetime / "restart" scenarios)
/// - FakeEncryptionKeyProvider (no ENCRYPTION_KEY env dependency)
/// - CapturingEmailSender (no SendGrid / network access)
/// </summary>
public sealed class AppTestFactory : WebApplicationFactory<Program>, IDisposable
{
    public FakeEncryptionKeyProvider KeyProvider { get; } = new();
    public CapturingEmailSender EmailSender { get; } = new();
    private readonly SqliteConnection? _externalConnection;
    private readonly SqliteConnection? _ownedConnection;

    /// <summary>Creates a factory with its own fresh in-memory SQLite database.</summary>
    public AppTestFactory()
    {
        _ownedConnection = new SqliteConnection("Data Source=:memory:");
        _ownedConnection.Open();
    }

    /// <summary>Creates a factory that uses an EXISTING open SQLite connection.
    /// Lets a second factory simulate an application restart against the same database.</summary>
    public AppTestFactory(SqliteConnection existingConnection)
    {
        _externalConnection = existingConnection;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Program.cs unconditionally builds an Npgsql data source from this connection
        // string; a syntactically valid string keeps that path happy without connecting.
        builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=testdb;Username=test;Password=test");

        builder.ConfigureServices(services =>
        {
            // Quiet the EF/Identity SQL logs in test output
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            // Strip the production Npgsql registration (EF Core 9 stores the provider
            // config in IDbContextOptionsConfiguration<T> alongside DbContextOptions<T>).
            for (var i = services.Count - 1; i >= 0; i--)
            {
                var st = services[i].ServiceType;
                var remove =
                    st == typeof(DbContextOptions<UnMuddledDbContext>) ||
                    st == typeof(DbContextOptions) ||
                    (st.IsGenericType
                        && st.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                        && st.GetGenericArguments()[0] == typeof(UnMuddledDbContext));
                if (remove) services.RemoveAt(i);
            }

            services.AddDbContext<UnMuddledDbContext>(o => o.UseSqlite(_externalConnection ?? _ownedConnection!));

            services.RemoveAll<IEncryptionKeyProvider>();
            services.AddSingleton<IEncryptionKeyProvider>(KeyProvider);

            services.RemoveAll<IEmailSender<User>>();
            services.AddSingleton<IEmailSender<User>>(EmailSender);
        });
    }

    /// <summary>Scope factory convenience for resolving app services (UserManager, DbContext...).</summary>
    public AsyncServiceScope CreateTestScope() => Services.CreateAsyncScope();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ownedConnection?.Dispose();
        }
        base.Dispose(disposing);
    }
}
