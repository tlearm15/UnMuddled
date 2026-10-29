using GroupProject.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
//WM - Adding entity framework core for database ops
using Microsoft.EntityFrameworkCore;


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
                List<string> roles = new List<string> { "Admin", "Staff", "Member", "SuspendedMember" };

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
                        UserRole = UserRole.Admin,
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

                    var staff = new User
                    {
                        UserName = "staff@unmuddled.com",
                        NormalizedUserName = "staff@unmuddled.com".ToUpper(),
                        Email = "staff@unmuddled.com",
                        NormalizedEmail = "staff@unmuddled.com".ToUpper(),
                        EmailConfirmed = true,
                        UserRole = UserRole.Staff,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        FirstName = "Jane",
                        LastName = "Doe",
                        AddressLine1 = "123 Staff St",
                        AddressLine2 = "Suite 100",
                        City = "Staff City",
                        PostCode = "ST1 2BC"
                    };
                    var staffResult = await userManager.CreateAsync(staff, "Staff1234!");
                    if (staffResult.Succeeded)
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

                    var member = new User
                    {
                        UserName = "member@gmail.com",
                        NormalizedUserName = "member@gmail.com".ToUpper(),
                        Email = "member@gmail.com",
                        NormalizedEmail = "member@gmail.com".ToUpper(),
                        EmailConfirmed = true,
                        UserRole = UserRole.Member,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        FirstName = "James",
                        LastName = "Doe",
                        AddressLine1 = "123 User St",
                        AddressLine2 = "Suite 100",
                        City = "User City",
                        PostCode = "US1 2BC"
                    };
                    var memberResult = await userManager.CreateAsync(member, "Member1234!");
                    if (memberResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(member, "Member");
                        logger.LogInformation("Member user created and assigned to Member role.");
                    }
                    else
                    {
                        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                        logger.LogError($"Error creating user user: {errors}");
                        throw new InvalidOperationException($"Error creating 'Member' user: {errors}");
                    }
                }
                logger.LogInformation("Seeding e-waste categories, locations, and history.");

                if (!context.Categories.Any())
                {
                    context.Categories.AddRange(
                        new Category { CategoryName = "Small Electronics" },
                        new Category { CategoryName = "Large Appliances" },
                        new Category { CategoryName = "Mobile Devices" }
                    );
                    await context.SaveChangesAsync();
                }

                if (!context.DisposalLocations.Any())
                {
                    context.DisposalLocations.AddRange(
                        new DisposalLocation { LocationName = "Glasgow Central Recycling", Town = "Glasgow", Postcode = "G1 1AA", Latitude = 55.8642m, Longitude = -4.2518m },
                        new DisposalLocation { LocationName = "Local E-Waste Drop-off", Town = "Glasgow", Postcode = "G2 2BB", Latitude = 55.8600m, Longitude = -4.2500m }
                    );
                    await context.SaveChangesAsync();
                }

                //WM - temporarily seeding some products for user to show a history of disposed items for testing.
                //WM - specifically async to avoid any blockages
                var normalUser = await userManager.FindByEmailAsync("user@gmail.com");//May need to change to 'member' to reflect new account. Member not seeded due to admin still present tho -RW
                if (normalUser != null && !await context.ProductHistories.AnyAsync(ph => ph.UserID == normalUser.Id))
                {
                    var userHistory = new ProductHistory
                    {
                        UserID = normalUser.Id
                    };
                    context.ProductHistories.Add(userHistory);
                    await context.SaveChangesAsync();

                    var catElectronics = context.Categories.First(c => c.CategoryName == "Small Electronics");
                    var catMobile = context.Categories.First(c => c.CategoryName == "Mobile Devices");
                    //var catLaptop = context.Categories.First(c => c.CategoryName == "Laptops");
                    //var catComputer = context.Categories.First(c => c.CategoryName == "Computers");
                    var locCentral = context.DisposalLocations.First(l => l.LocationName == "Glasgow Central Recycling");

                    var item1 = new Item
                    {
                        ItemName = "Broken Toaster",
                        Description = "2-slice toaster, heating element burned out.",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Kitchen,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-2)
                    };

                    var item2 = new Item
                    {
                        ItemName = "Old Smartphone",
                        Description = "Screen cracked, battery swollen. Responsibly disposed.",
                        IsRecyclable = true,
                        CategoryID = catMobile.CategoryID,
                        RoomTag = RoomTag.Bedroom,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-5)
                    };

                    var item3 = new Item
                    {
                        ItemName = "HP Laptop",
                        Description = "Slowing down, Short battery life.",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Office,
                        HistoryID = userHistory.HistoryID,
                        DateAdded= DateTime.UtcNow.AddDays(-6)

                    };

                    var item4 = new Item
                    {
                        ItemName = "Dell Computer",
                        Description = "Overheating, clogged fans & long start up",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Office,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow

                    };

                    var item5 = new Item
                    {
                        ItemName = "IPhone Duo",
                        Description = "Broken in 2 at the hinge",
                        IsRecyclable = true,
                        CategoryID = catMobile.CategoryID,
                        RoomTag = RoomTag.Bedroom,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow
                    };

                    var item6 = new Item
                    {
                        ItemName = "Samsung Smart Fridge",
                        Description = "Large crack in cooling unit",
                        IsRecyclable = false,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Kitchen,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-10)
                    };

                    var item7 = new Item
                    {
                        ItemName = "Smoke Detector",
                        Description = "Water damaged from flooding",
                        IsRecyclable = false,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Other,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-2)
                    };

                    var item8 = new Item
                    {
                        ItemName = "LCD Television",
                        Description = "Spider web cracking from middle out",
                        IsRecyclable = false,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.LivingRoom,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-1)
                    };
                    context.Items.AddRange(item1, item2, item3, item4, item5, item6, item7, item8);
                    await context.SaveChangesAsync();

                    context.ItemDisposals.AddRange(
                        new ItemDisposal { ItemID = item1.ItemID, DisposalID = locCentral.DisposalID },
                        new ItemDisposal { ItemID = item2.ItemID, DisposalID = locCentral.DisposalID }
                    );
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");

            }
        }
    }
}
