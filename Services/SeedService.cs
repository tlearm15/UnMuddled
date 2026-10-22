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
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();

            try
            {
                logger.LogInformation("Ensure database is created.");
                await context.Database.EnsureCreatedAsync();

                logger.LogInformation("Seeding roles.");
                List<string> roles = new List<string> { "Admin", "Staff", "User" };

                //Creates the roles -RW
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        var result = await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation($"Role '{role}' created.");
                        // Log errors if role creation fails -RW
                        if (!result.Succeeded)
                        {
                            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                            logger.LogError($"Error creating role '{role}': {errors}");
                            throw new InvalidOperationException($"Error creating role '{role}': {errors}");
                        }
                    }
                }

                logger.LogInformation("Seeding users.");
                // Create an admin user if it doesn't exist -RW
                var adminEmail = "admin@unmuddled.com";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var admin = new User
                    {
                        UserName = adminEmail,
                        NormalizedUserName = adminEmail.ToUpper(),
                        Email = adminEmail,
                        NormalizedEmail = adminEmail.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        FirstName = "John",
                        LastName = "Doe",
                        AddressLine1 = "123 Admin St",
                        AddressLine2 = "Suite 100",
                        City = "Admin City",
                        PostCode = "AD1 2BC"
                    };
                    var result = await userManager.CreateAsync(admin, "Admin1234!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                        logger.LogInformation("Admin user created and assigned to Admin role.");
                    }
                    else
                    {
                        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                        logger.LogError($"Error creating admin user: {errors}");
                        throw new InvalidOperationException($"Error creating admin user: {errors}");
                    }
                    //Create Staff user if it doesn't exist -RW
                    var staff = new User
                    {
                        UserName = "staff@unmuddled.com",
                        NormalizedUserName = "staff@unmuddled.com".ToUpper(),
                        Email = "staff@unmuddled.com",
                        NormalizedEmail = "staff@unmuddled.com".ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        FirstName = "Jane",
                        LastName = "Doe",
                        AddressLine1 = "123 Staff St",
                        AddressLine2 = "Suite 100",
                        City = "Staff City",
                        PostCode = "ST1 2BC"
                    };
                    var staffResult = await userManager.CreateAsync(staff, "Staff1234!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(staff, "Staff");
                        logger.LogInformation("Staff user created and assigned to Staff role.");
                    }
                    else
                    {
                        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                        logger.LogError($"Error creating staff user: {errors}");
                        throw new InvalidOperationException($"Error creating staff user: {errors}");
                    }
                    //Create User user if it doesn't exist -RW
                    var user = new User
                    {
                        UserName = "user@gmail.com",
                        NormalizedUserName = "user@gmail.com".ToUpper(),
                        Email = "user@gmail.com",
                        NormalizedEmail = "user@gmail.com".ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        FirstName = "James",
                        LastName = "Doe",
                        AddressLine1 = "123 User St",
                        AddressLine2 = "Suite 100",
                        City = "User City",
                        PostCode = "US1 2BC"
                    };
                    var customerResult = await userManager.CreateAsync(user, "User1234!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "User");
                        logger.LogInformation("User user created and assigned to User role.");
                    }
                    else
                    {
                        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                        logger.LogError($"Error creating user user: {errors}");
                        throw new InvalidOperationException($"Error creating 'User' user: {errors}");
                    }
                }
            }
            catch (Exception ex)
            {// Log any exceptions that occur during the seeding process
                logger.LogError(ex, "An error occurred while seeding the database.");

            }
        }
    }
}
