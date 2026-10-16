using GroupProject.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GroupProject.Services
{
    public class SeedService
    {
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            // Create a scope to get the required services
            using var scope = serviceProvider.CreateScope();
            // Get the required services from the scope
            var context = scope.ServiceProvider.GetRequiredService<UnMuddledDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();// TQ- dead code, for now
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();

            try
            {
                logger.LogInformation("Ensure database is created.");
                await context.Database.EnsureCreatedAsync();

                logger.LogInformation("Seeding roles.");
                //await AddRoles(roleManager, "Admin");//FIX THIS. ADD ROLES NOT FOUND-RW to fix
                // TQ- just directly invoke the roleManager methods.
                if( !await roleManager.RoleExistsAsync( "Admin" ) )// Create Admin role, if that wasn't already obvious
                    await roleManager.CreateAsync(new IdentityRole( "Admin" ) );

                if (!await roleManager.RoleExistsAsync("Staff"))// Create Staff role, ^^^^
                    await roleManager.CreateAsync(new IdentityRole("Staff"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
    }
}
