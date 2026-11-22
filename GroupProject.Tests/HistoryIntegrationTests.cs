using GroupProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Xunit;

namespace GroupProject.Tests
{
    //WM - this is setting up a fake test server stored in the memory for integration testing, not on the real server or database.
    public class HistoryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        //WM - this is making an instance of web application factory, it's a test server to host the application in-memory.
        private readonly WebApplicationFactory<Program> _factory;

        public HistoryIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // WM - setting a variable for the encryption key so the test server works properly as it expects the key
            Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            _factory = factory;
        }

        //WM - creating a test server with a database in-memory so that it doesn't need the actual database
        private WebApplicationFactory<Program> CreateTestFactory(string dbName, bool authenticateUser = false)
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // WM - removing the original dbcontext to replace it with the in memory database for testing
                    var dbDescriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<UnMuddledDbContext>) ||
                        d.ServiceType == typeof(IDbContextOptionsConfiguration<UnMuddledDbContext>)).ToList();

                    foreach (var descriptor in dbDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<UnMuddledDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    // WM - if the user is authenticated then use a fake handler to simulate the authentication
                    if (authenticateUser)
                    {
                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = "TestScheme";
                            options.DefaultChallengeScheme = "TestScheme";
                        })
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });
                    }
                });
            });
        }

        // T-12.4: testing that a user cannot access the history page without being signed in
        [Fact]
        public async Task T12_4_GetHistory_Unauthenticated_RedirectsToLogin()
        {
            var factory = CreateTestFactory(Guid.NewGuid().ToString(), authenticateUser: false);
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false // preventing the automated redirection so the response can be recorded
            });

            var response = await client.GetAsync("/History");

            //WM - checking the response is a redirect to login, the expected result
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
        }

        // T-13 & T-14: testing that a user has their own history page and it shows the correct items plus count of items
        [Fact]
        public async Task T13_T14_GetHistory_Authenticated_RendersHtmlWithItemsAndCount()
        {
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateTestFactory(dbName, authenticateUser: true);

            // WM - seeding the db with a test user and test item so that the history page ahs something for them.
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<UnMuddledDbContext>();

                if (!await context.Users.AnyAsync(u => u.Id == "test-user-1"))
                {
                    context.Users.Add(new User
                    {
                        Id = "test-user-1",
                        UserName = "test@unmuddled.com",
                        Email = "test@unmuddled.com",
                        FirstName = "Test",
                        LastName = "User",
                        AddressLine1 = "123 Test St",
                        City = "Glasgow",
                        PostCode = "G1 1AA"
                    });
                }
                var seededItem = await context.Items.FirstAsync(i => i.ItemName == "Broken Toaster");
                var testHistory = new UserHistory { UserId = "test-user-1" };

                context.UserHistories.Add(testHistory);
                await context.SaveChangesAsync();

                context.UserHistoryItems.AddRange(
                    new UserHistoryItem { ItemId = seededItem.ItemID, UserHistoryId = testHistory.UserHistoryId, DateDisposed = DateTime.UtcNow.AddDays(-1) },
                    new UserHistoryItem { ItemId = seededItem.ItemID, UserHistoryId = testHistory.UserHistoryId, DateDisposed = DateTime.UtcNow }
                );
                await context.SaveChangesAsync();
            }

            var client = factory.CreateClient();
            var response = await client.GetAsync("/History");
            var html = await response.Content.ReadAsStringAsync();

            //WM - checking that the response is OK for the user accessing their page
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            //WM - checking to make sure the html content displays the information expected, including the item and number of items.
            Assert.Contains("My Recycling History", html);
            Assert.Contains("Broken Toaster", html);
            Assert.Contains("Total Items Recycled: 2", html);
        }

        // T-13 (Security): Testing that a user can't access the history page of another user via url.
        [Fact]
        public async Task T13_GetHistory_OtherUserIdAsMember_ReturnsForbidden()
        {
            var factory = CreateTestFactory(Guid.NewGuid().ToString(), authenticateUser: true);
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync("/History?userId=someone-else-999");

            //WM - checking the response is forbidden as expected.
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    //WM - faked authentication handler to spoof a logged in user for testing.
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-1"),
                new Claim(ClaimTypes.Name, "member@gmail.com"),
                new Claim(ClaimTypes.Role, "Member")
            };
            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}