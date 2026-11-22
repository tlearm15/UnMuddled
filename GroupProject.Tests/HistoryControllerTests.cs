using GroupProject.Controllers;
using GroupProject.Models;
using GroupProject.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GroupProject.Tests
{
    public class HistoryControllerTests
    {
        // WM - helper methods to set up the mock database and user manager to test the controller without touching the actual database or users.

        private UnMuddledDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<UnMuddledDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var mockKeyProvider = new Mock<IEncryptionKeyProvider>();
            var mockEncryptionService = new Mock<IEncryptionService>();

            return new UnMuddledDbContext(options, mockKeyProvider.Object, mockEncryptionService.Object);
        }

        // WM helper method for the mock user manager that will simulate a user in the database.
        private Mock<UserManager<User>> GetMockUserManager(string? userId)
        {
            var store = new Mock<IUserStore<User>>();
            var userManager = new Mock<UserManager<User>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            userManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                       .Returns(userId);

            return userManager;
        }

        // WM - another helper method but this time to prepare user context to simulate a logged in user.
        private void SetUserContext(HistoryController controller, string? userId, string role = "Member")
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        // WM - tests are performed below for each of the requirements listed on the testing index

        // WM - T-12.1: Test that when a user marks an item as recycled, it is appropriately recorded on their profile.
        //this code ensures that when an item added to the recycling history, it;s correctly recorded and the user is redirected to their history page.
        //it checks the database after to confirm it was added.
        [Fact]
        public async Task T12_1_AddToHistory_ValidItem_RecordedInDatabase()
        {
            using var context = GetInMemoryDbContext();
            context.Items.Add(new Item { ItemID = 1, ItemName = "Old Laptop", Description = "Broken screen" });
            await context.SaveChangesAsync();

            var controller = new HistoryController(context, GetMockUserManager("user-1").Object);
            SetUserContext(controller, "user-1");

            var result = await controller.AddToHistory(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("History", redirect.ControllerName);
            Assert.Equal(1, await context.UserHistoryItems.CountAsync());
        }

        // WM - T-12.2: Delete an item from recycling history
        //this code makes sure that when an item is deleted it is actually removed from the db.
        //it sets up a user with one item in their history and then calls remove from history to remove it.
        [Fact]
        public async Task T12_2_RemoveFromHistory_ValidItem_DeletedFromDatabase()
        {
            using var context = GetInMemoryDbContext();
            var history = new UserHistory { UserHistoryId = 1, UserId = "user-1" };
            context.UserHistories.Add(history);
            context.UserHistoryItems.Add(new UserHistoryItem { ItemId = 1, UserHistoryId = 1, DateDisposed = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var controller = new HistoryController(context, GetMockUserManager("user-1").Object);
            SetUserContext(controller, "user-1");

            var result = await controller.RemoveFromHistory(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(0, await context.UserHistoryItems.CountAsync());
        }

        // WM - T-12.3: Recycle the same item twice and verify both entries are recorded
        //this code checks that if a user recycles the same item twice, both entries are properly recorded into the db.
        //this makes sure that duplicates are recorded individually.
        [Fact]
        public async Task T12_3_AddToHistory_SameItemTwice_RecordsDuplicates()
        {
            using var context = GetInMemoryDbContext();
            context.Items.Add(new Item { ItemID = 1, ItemName = "AA Batteries", Description = "Dead batteries" });
            await context.SaveChangesAsync();

            var controller = new HistoryController(context, GetMockUserManager("user-1").Object);
            SetUserContext(controller, "user-1");

            await controller.AddToHistory(1);
            await controller.AddToHistory(1);

            var count = await context.UserHistoryItems.CountAsync(uhi => uhi.ItemId == 1);
            Assert.Equal(2, count);
        }

        //WM - T-12.4: User can only recycle when logged in
        //this code ensures that a user can't add an item to their recycling history if they're not logged in.
        //they should be redirected to the login page instead.
        [Fact]
        public async Task T12_4_AddToHistory_LoggedOutUser_RedirectsToLogin()
        {
            using var context = GetInMemoryDbContext();
            var controller = new HistoryController(context, GetMockUserManager(null).Object);
            SetUserContext(controller, null);

            var result = await controller.AddToHistory(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        // WM - T-12.5: System handles trying to recycle a non-existent item
        //this code is checking that when a user tries to add an item that doesnt exist to their recycling history it returns not found instead of crashing or something else.
        [Fact]
        public async Task T12_5_AddToHistory_NonExistentItem_ReturnsNotFound()
        {
            using var context = GetInMemoryDbContext();
            var controller = new HistoryController(context, GetMockUserManager("user-1").Object);
            SetUserContext(controller, "user-1");

            var result = await controller.AddToHistory(99999);

            // WM - making sure the result is not found as expected.
            Assert.IsType<NotFoundResult>(result);
        }

        // WM - T-13: Users have their own individual recycling history page (and cannot view others)
        //this code checks that when a user views their recycling history page, the can only see their own items.
        //it sets up two users with items and makes sure user a only sees their own items.
        [Fact]
        public async Task T13_Index_ReturnsOnlyLoggedInUsersHistory_AndForbidsOthers()
        {
            using var context = GetInMemoryDbContext();
            context.Items.AddRange(
                new Item { ItemID = 1, ItemName = "Toaster", Description = "User A item" },
                new Item { ItemID = 2, ItemName = "Kettle", Description = "User B item" }
            );
            context.UserHistories.AddRange(
                new UserHistory { UserHistoryId = 1, UserId = "user-A" },
                new UserHistory { UserHistoryId = 2, UserId = "user-B" }
            );
            context.UserHistoryItems.AddRange(
                new UserHistoryItem { ItemId = 1, UserHistoryId = 1, DateDisposed = DateTime.UtcNow },
                new UserHistoryItem { ItemId = 2, UserHistoryId = 2, DateDisposed = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var controller = new HistoryController(context, GetMockUserManager("user-A").Object);
            SetUserContext(controller, "user-A", "Member");

            var ownResult = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(ownResult);
            var model = Assert.IsAssignableFrom<List<UserHistoryItem>>(viewResult.Model);
            // WM - asserting that the model only shows the items for A and not B and confirms the count is correct.
            Assert.Single(model);
            Assert.Equal(1, model[0].ItemId);

            var otherResult = await controller.Index("user-B");

            //WM - making sure a forbidden result is returned
            Assert.IsType<ForbidResult>(otherResult);
        }

        // WM - T-14: Recycling history page correctly counts total items recycled
        //this code checks that the recycling history page displays the correct count of items recycled by that single user.
        //three items are added and then count is checked.
        [Fact]
        public async Task T14_Index_ReturnsAccurateTotalItemCount()
        {
            using var context = GetInMemoryDbContext();
            context.Items.Add(new Item { ItemID = 1, ItemName = "Phone Charger", Description = "USB-C" });
            context.UserHistories.Add(new UserHistory { UserHistoryId = 1, UserId = "user-1" });
            context.UserHistoryItems.AddRange(
                new UserHistoryItem { ItemId = 1, UserHistoryId = 1, DateDisposed = DateTime.UtcNow.AddDays(-2) },
                new UserHistoryItem { ItemId = 1, UserHistoryId = 1, DateDisposed = DateTime.UtcNow.AddDays(-1) },
                new UserHistoryItem { ItemId = 1, UserHistoryId = 1, DateDisposed = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var controller = new HistoryController(context, GetMockUserManager("user-1").Object);
            SetUserContext(controller, "user-1");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<UserHistoryItem>>(viewResult.Model);
            // WM - making sure count of items in the model is 3 as expected.
            Assert.Equal(3, model.Count);
        }
    }
}