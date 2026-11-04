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

                if(normalUser == null)
                {//Ensures the User Exists before attempting to seed Histroy - RW
                    throw new InvalidOperationException("Normal user not found. Ensure the user is seeded before seeding product history.");
                }

                //Checks if the user has a history already, if not creates one. - RW
                var userHistory = await context.ProductHistories.FirstOrDefaultAsync(ph => ph.UserID == normalUser.Id);

                if(userHistory == null)
                {
                    userHistory = new ProductHistory
                    {
                        UserID = normalUser.Id
                    };
                    context.ProductHistories.Add(userHistory);
                    await context.SaveChangesAsync();
                }
                


                //**Start of Item Seeding** - RW
                //Seeding note - Most Household Electricals are recyclable. Some items listed are not, this is for testing prior to being overly specific
                //as certain items if Fire Damaged can no longer be recycled. - RW
                var catElectronics = context.Categories.First(c => c.CategoryName == "Small Electronics");
                var catAppliances = context.Categories.First(c => c.CategoryName == "Large Appliances");
                var catMobile = context.Categories.First(c => c.CategoryName == "Mobile Devices");
                var locCentral = context.DisposalLocations.First(l => l.LocationName == "Glasgow Central Recycling");

                //Checks if the item exists before adding to avoid duplicates
                //This boolean is repeated for each item to ensure that the seeding process does not create duplicates if run multiple times. - RW
                bool toasterExists =  await context.Items.AnyAsync(i => i.ItemName == "Broken Toaster" && i.HistoryID == userHistory.HistoryID);
                if(!toasterExists)
                {
                    var item1 = new Item
                    {
                        ItemName = "Broken Toaster",
                        Description = "Ensure Appliance is Cool. Damaged Heating Element will not prevent Recycling. Take to nearest Recycling centre",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Kitchen,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-2)
                    };
                    context.Items.Add(item1);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item1, DisposalID = locCentral.DisposalID });
                }
               
                bool smartphoneExists = await context.Items.AnyAsync(i => i.ItemName == "Old Smartphone" && i.HistoryID == userHistory.HistoryID);
                if (!smartphoneExists)
                {
                    var item2 = new Item
                    {
                        ItemName = "Old Smartphone",
                        Description = "If there is any obvious battery damage, do not attempt to power on or plug device in. Contact recycling centre and disclose any damage and follow advice.",
                        IsRecyclable = true,
                        CategoryID = catMobile.CategoryID,
                        RoomTag = RoomTag.Bedroom,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-5)
                    };
                    context.Items.Add(item2);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item2, DisposalID = locCentral.DisposalID });
                }
                
                bool laptopExists = await context.Items.AnyAsync(i => i.ItemName == "HP Laptop" && i.HistoryID == userHistory.HistoryID);
                if(!laptopExists)
                {
                    var item3 = new Item
                    {
                        ItemName = "HP Laptop",
                        Description = "If battery can be removed safely, please do so. Then take to Recycling Centre of WEEE Collection point to be recycled",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Office,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-6)
                    };
                    context.Items.Add(item3);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item3, DisposalID = locCentral.DisposalID });
                }
                
                bool dellComputerExists = await context.Items.AnyAsync(i => i.ItemName == "Dell Computer" && i.HistoryID == userHistory.HistoryID);
                if(!dellComputerExists)
                {
                    var item4 = new Item
                    {
                        ItemName = "Dell Computer",
                        Description = "Do not dispose in house bins. Take to Electronics Recycling Centre or WEEE Collection Point ",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Office,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow
                    };
                    context.Items.Add(item4);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item4, DisposalID = locCentral.DisposalID });
                }
               
                bool iphoneExists = await context.Items.AnyAsync(i => i.ItemName == "IPhone Duo" && i.HistoryID == userHistory.HistoryID);
                if(!iphoneExists)
                {
                    var item5 = new Item
                    {
                        ItemName = "IPhone Duo",
                        Description = "If any battery damage, do not power on or attempt to plug device in. Contact Recycling Centre and report any Battery Damage and follow advice for recycling.",
                        IsRecyclable = true,
                        CategoryID = catMobile.CategoryID,
                        RoomTag = RoomTag.Bedroom,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow
                    };
                    context.Items.Add(item5);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item5, DisposalID = locCentral.DisposalID });
                }
                
                bool fridgeExists =  await context.Items.AnyAsync(i => i.ItemName == "Samsung Smart Fridge" && i.HistoryID == userHistory.HistoryID);
                if (!fridgeExists)
                {
                    var item6 = new Item
                    {
                        ItemName = "Samsung Smart Fridge",
                        Description = "Only to be Recycled at Authorized Centres, Retailers or Take-back services. Check with Manufacturer or Local Recycling centres for adivce.",
                        IsRecyclable = false,
                        CategoryID = catAppliances.CategoryID,
                        RoomTag = RoomTag.Kitchen,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-10)
                    };
                    context.Items.Add(item6);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item6, DisposalID = locCentral.DisposalID });
                }
                
                bool smokeDetectorExists =  await context.Items.AnyAsync(i => i.ItemName == "Smoke Detector" && i.HistoryID == userHistory.HistoryID);
                if (!smokeDetectorExists)
                {
                    var item7 = new Item
                    {
                        ItemName = "Smoke Detector",
                        Description = "CAUTION:Some Smoke Detectors contain small amounts of Radioactive material.\nIf safe to do so, remove battery. DO NOT, needlessly dismantle. Take to Recycling Centre that accepts Smoke Alarms to ensure proper, safe disposal.",
                        IsRecyclable = false,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.Other,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-2)
                    };
                    context.Items.Add(item7);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item7, DisposalID = locCentral.DisposalID });
                }
                
                bool lcdTvExists =  await context.Items.AnyAsync(i => i.ItemName == "LCD Television" && i.HistoryID == userHistory.HistoryID);
                if(!lcdTvExists)
                {
                    var item8 = new Item
                    {
                        ItemName = "LCD Television",
                        Description = "Do not place in general Electronics Collections. Take to Recycling Centre that accepts these Screen Types to ensure safe disposal",
                        IsRecyclable = false,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.LivingRoom,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-1)
                    };
                    context.Items.Add(item8);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item8, DisposalID = locCentral.DisposalID });
                }
                
                bool dvdPlayerExists =  await context.Items.AnyAsync(i => i.ItemName == "DvD Player" && i.HistoryID == userHistory.HistoryID);
                if(!dvdPlayerExists)
                {
                    var item9 = new Item
                    {
                        ItemName = "DvD Player",
                        Description = "If safe to do so, remove any disk from player. DO NOT Dismantle player. Take to Recycling Centre for Recycling.",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.LivingRoom,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow
                    };
                    context.Items.Add(item9);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item9, DisposalID = locCentral.DisposalID });
                }
                
                bool tvRemoteExists = await context.Items.AnyAsync(i => i.ItemName == "TV Remote" && i.HistoryID == userHistory.HistoryID);
                if(!tvRemoteExists)
                {
                    var item10 = new Item
                    {
                        ItemName = "TV Remote",
                        Description = "Remove Batteries and Dispose in Battery collection points. Take Remote to small electricals facility",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.LivingRoom,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-5)
                    };
                    context.Items.Add(item10);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item10, DisposalID = locCentral.DisposalID });
                }
                
                bool ps3Exists =  await context.Items.AnyAsync(i => i.ItemName == "PS3" && i.HistoryID == userHistory.HistoryID);
                if(!ps3Exists)
                {
                    var item11 = new Item
                    {
                        ItemName = "PS3",
                        Description = "If safe to do so, remove any Disk inside console. DO NOT Dismantle. Take to Electrical Recycling Centre.",
                        IsRecyclable = true,
                        CategoryID = catElectronics.CategoryID,
                        RoomTag = RoomTag.LivingRoom,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-7)
                    };
                    context.Items.Add(item11);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item11, DisposalID = locCentral.DisposalID });
                }

                bool acUnitExists =  await context.Items.AnyAsync(i => i.ItemName == "Air Conditioning Unit" && i.HistoryID == userHistory.HistoryID);
                if(!acUnitExists)
                {
                    var item12 = new Item
                    {
                        ItemName = "Air Conditioning Unit",
                        Description = "Contact Recycling Centre that accepts these units and follow advice given.\nCAUTION: Do not attempt to move due to Toxi Refrigerants. DO NOT Cut Pipes or Dismantle any part of Unit.",
                        IsRecyclable = false,
                        CategoryID = catAppliances.CategoryID,
                        RoomTag = RoomTag.Other,
                        HistoryID = userHistory.HistoryID,
                        DateAdded = DateTime.UtcNow.AddDays(-11)
                    };
                    context.Items.Add(item12);
                    context.ItemDisposals.Add(new ItemDisposal { Item = item12, DisposalID = locCentral.DisposalID });
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
