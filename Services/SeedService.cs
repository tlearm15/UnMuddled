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
                var normalUser = await userManager.FindByEmailAsync("user@gmail.com");

                UserHistory userHistory = null;
                if (normalUser != null)
                {
                    userHistory = await context.UserHistories.FirstOrDefaultAsync(uh => uh.UserId == normalUser.Id);
                    if (userHistory == null)
                    {
                        userHistory = new UserHistory
                        {
                            UserId = normalUser.Id
                        };
                        context.UserHistories.Add(userHistory);
                        await context.SaveChangesAsync();
                    }
                }

                //**Start of Item Seeding** - RW
                var catElectronics = context.Categories.First(c => c.CategoryName == "Small Electronics");
                var catAppliances = context.Categories.First(c => c.CategoryName == "Large Appliances");
                var catMobile = context.Categories.First(c => c.CategoryName == "Mobile Devices");
                var locCentral = context.DisposalLocations.First(l => l.LocationName == "Glasgow Central Recycling");

                Item item1 = null, item2 = null, item3 = null, item4 = null, item5 = null, item6 = null, item7 = null, item8 = null, item9 = null, item10 = null, item11 = null, item12 = null;

                bool item1Exists = await context.Items.AnyAsync(i => i.ItemName == "Broken Toaster");
                if (!item1Exists)
                {
                    item1 = new Item
                    {
                        ItemName = "Broken Toaster",
                        Description = "Ensure Appliance is Cool. Damaged Heating Element will not prevent Recycling. Take to nearest Recycling centre",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Kitchen,
                        DateAdded = DateTime.UtcNow.AddDays(-2)
                    };
                    context.Items.Add(item1);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item1, DisposalID = locCentral.DisposalID });
                }

                bool item2Exists = await context.Items.AnyAsync(i => i.ItemName == "Old Smartphone");
                if (!item2Exists)
                {
                    item2 = new Item
                    {
                        ItemName = "Old Smartphone",
                        Description = "If there is any obvious battery damage, do not attempt to power on or plug device in. Contact recycling centre and disclose any damage and follow advice.",
                        IsRecyclable = true,
                        CategoryID = catMobile.CategoryID,
                        RoomTag = RoomTag.Bedroom,
                        DateAdded = DateTime.UtcNow.AddDays(-5)
                    };
                    context.Items.Add(item2);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item2, DisposalID = locCentral.DisposalID });
                }

                bool item3Exists = await context.Items.AnyAsync(i => i.ItemName == "HP Laptop");
                if (!item3Exists)
                {
                    item3 = new Item
                    {
                        ItemName = "HP Laptop",
                        Description = "If battery can be removed safely, please do so. Then take to Recycling Centre of WEEE Collection point to be recycled",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Office,
                        DateAdded = DateTime.UtcNow.AddDays(-6)
                    };
                    context.Items.Add(item3);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item3, DisposalID = locCentral.DisposalID });
                }

                bool item4Exists = await context.Items.AnyAsync(i => i.ItemName == "Dell Computer");
                if (!item4Exists)
                {
                    item4 = new Item
                    {
                        ItemName = "Dell Computer",
                        Description = "Do not dispose in house bins. Take to Electronics Recycling Centre or WEEE Collection Point ",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Office,
                        DateAdded = DateTime.UtcNow
                    };
                    context.Items.Add(item4);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item4, DisposalID = locCentral.DisposalID });
                }

                bool item5Exists = await context.Items.AnyAsync(i => i.ItemName == "IPhone Duo");
                if (!item5Exists)
                {
                    item5 = new Item
                    {
                        ItemName = "IPhone Duo",
                        Description = "If any battery damage, do not power on or attempt to plug device in. Contact Recycling Centre and report any Battery Damage and follow advice for recycling.",
                        IsRecyclable = true,
                        CategoryID = catMobile.CategoryID,
                        RoomTag = RoomTag.Bedroom,
                        DateAdded = DateTime.UtcNow
                    };
                    context.Items.Add(item5);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item5, DisposalID = locCentral.DisposalID });
                }

                bool item6Exists = await context.Items.AnyAsync(i => i.ItemName == "Samsung Smart Fridge");
                if (!item6Exists)
                {
                    item6 = new Item
                    {
                        ItemName = "Samsung Smart Fridge",
                        Description = "Only to be Recycled at Authorized Centres, Retailers or Take-back services. Check with Manufacturer or Local Recycling centres for adivce.",
                        IsRecyclable = false,
                        CategoryID = catAppliances.CategoryID,
                        RoomTag = RoomTag.Kitchen,
                        DateAdded = DateTime.UtcNow.AddDays(-10)
                    };
                    context.Items.Add(item6);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item6, DisposalID = locCentral.DisposalID });
                }

                bool item7Exists = await context.Items.AnyAsync(i => i.ItemName == "Smoke Detector");
                if (!item7Exists)
                {
                    item7 = new Item
                    {
                        ItemName = "Smoke Detector",
                        Description = "CAUTION:Some Smoke Detectors contain small amounts of Radioactive material.\nIf safe to do so, remove battery. DO NOT, needlessly dismantle. Take to Recycling Centre that accepts Smoke Alarms to ensure proper, safe disposal.",
                        IsRecyclable = false,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Other,
                        DateAdded = DateTime.UtcNow.AddDays(-2)
                    };
                    context.Items.Add(item7);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item7, DisposalID = locCentral.DisposalID });
                }

                bool item8Exists = await context.Items.AnyAsync(i => i.ItemName == "LCD Television");
                if (!item8Exists)
                {
                    item8 = new Item
                    {
                        ItemName = "LCD Television",
                        Description = "Do not place in general Electronics Collections. Take to Recycling Centre that accepts these Screen Types to ensure safe disposal",
                        IsRecyclable = false,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.LivingRoom,
                        DateAdded = DateTime.UtcNow.AddDays(-1)
                    };
                    context.Items.Add(item8);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item8, DisposalID = locCentral.DisposalID });
                }

                bool item9Exists = await context.Items.AnyAsync(i => i.ItemName == "DvD Player");
                if (!item9Exists)
                {
                    item9 = new Item
                    {
                        ItemName = "DvD Player",
                        Description = "If safe to do so, remove any disk from player. DO NOT Dismantle player. Take to Recycling Centre for Recycling.",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.LivingRoom,
                        DateAdded = DateTime.UtcNow
                    };
                    context.Items.Add(item9);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item9, DisposalID = locCentral.DisposalID });
                }

                bool item10Exists = await context.Items.AnyAsync(i => i.ItemName == "TV Remote");
                if (!item10Exists)
                {
                    item10 = new Item
                    {
                        ItemName = "TV Remote",
                        Description = "Remove Batteries and Dispose in Battery collection points. Take Remote to small electricals facility",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.LivingRoom,
                        DateAdded = DateTime.UtcNow.AddDays(-5)
                    };
                    context.Items.Add(item10);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item10, DisposalID = locCentral.DisposalID });
                }

                bool item11Exists = await context.Items.AnyAsync(i => i.ItemName == "PS3");
                if (!item11Exists)
                {
                    item11 = new Item
                    {
                        ItemName = "PS3",
                        Description = "If safe to do so, remove any Disk inside console. DO NOT Dismantle. Take to Electrical Recycling Centre.",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.LivingRoom,
                        DateAdded = DateTime.UtcNow.AddDays(-7)
                    };
                    context.Items.Add(item11);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item11, DisposalID = locCentral.DisposalID });
                }

                bool item12Exists = await context.Items.AnyAsync(i => i.ItemName == "Air Conditioning Unit");
                if (!item12Exists)
                {
                    item12 = new Item
                    {
                        ItemName = "Air Conditioning Unit",
                        Description = "Contact Recycling Centre that accepts these units and follow advice given.\nCAUTION: Do not attempt to move due to Toxi Refrigerants. DO NOT Cut Pipes or Dismantle any part of Unit.",
                        IsRecyclable = false,
                        CategoryID = catAppliances.CategoryID,
                        RoomTag = RoomTag.Other,
                        DateAdded = DateTime.UtcNow.AddDays(-11)
                    };
                    context.Items.Add(item12);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item12, DisposalID = locCentral.DisposalID });
                }

                if (userHistory != null)
                {
                    await context.SaveChangesAsync();

                    // WM - creating the junction table so i can link items to the user's history and disposal locs
                    if (item1 != null && item2 != null && item3 != null && item4 != null && item5 != null && item6 != null && item7 != null && item8 != null && item9 != null && item10 != null && item11 != null && item12 != null)
                    {
                        context.UserHistoryItems.AddRange(
                            new UserHistoryItem { ItemId = item1.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item1.DateAdded },
                            new UserHistoryItem { ItemId = item2.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item2.DateAdded },
                            new UserHistoryItem { ItemId = item3.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item3.DateAdded },
                            new UserHistoryItem { ItemId = item4.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item4.DateAdded },
                            new UserHistoryItem { ItemId = item5.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item5.DateAdded },
                            new UserHistoryItem { ItemId = item6.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item6.DateAdded },
                            new UserHistoryItem { ItemId = item7.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item7.DateAdded },
                            new UserHistoryItem { ItemId = item8.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item8.DateAdded },
                            new UserHistoryItem { ItemId = item9.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item9.DateAdded },
                            new UserHistoryItem { ItemId = item10.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item10.DateAdded },
                            new UserHistoryItem { ItemId = item11.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item11.DateAdded },
                            new UserHistoryItem { ItemId = item12.ItemID, UserHistoryId = userHistory.UserHistoryId, DateDisposed = item12.DateAdded }
                        );
                        await context.SaveChangesAsync();
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
    }
}
