// WM - Controller for displaying and processing the user's recycled items history

using GroupProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Controllers;

[Authorize]
public class HistoryController : Controller
{
    private readonly UnMuddledDbContext _context;
    private readonly UserManager<User> _userManager;

    // WM - initializing the controller with the database context and user manager
    public HistoryController(UnMuddledDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /History
    //WM - displaying the user's recycled items
    //WM - passes in the userid as an optional paramater for viewing a user's recycled product as an admin
    public async Task<IActionResult> Index(string? userId = null)
    {
        //WM - if there's no passed in user id then grab the logged in user's id.
        if (string.IsNullOrEmpty(userId))
        {
            userId = _userManager.GetUserId(User);
        }

        //WM - if they arent logged in, redirect to login
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        //WM - if the user's trying to view a history that isn't their own and they aren't an admin, prevent  access
        if (userId != _userManager.GetUserId(User) && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            return Forbid();
        }

        // WM - getting the user's product history by doing navigation through the junction table so
        // this grabs recycled items, categories, images, and disposal locatations
        var userHistory = await _context.UserHistories
            .Include(uh => uh.UserHistoryItems)
                .ThenInclude(uhi => uhi.Item)
                    .ThenInclude(i => i.Category)
            .Include(uh => uh.UserHistoryItems)
                .ThenInclude(uhi => uhi.Item)
                    .ThenInclude(i => i.Images)
            .Include(uh => uh.UserHistoryItems)
                .ThenInclude(uhi => uhi.Item)
                    .ThenInclude(i => i.ItemDisposals)
                        .ThenInclude(id => id.DisposalLocation)
            .FirstOrDefaultAsync(uh => uh.UserId == userId);

        //WM - display empty list if there's no history or items yet
        if (userHistory == null || userHistory.UserHistoryItems == null)
        {
            return View(new List<UserHistoryItem>());
        }

        // WM - getting recycled items ordered by desc of disposal
        var recycledItems = userHistory.UserHistoryItems
            .OrderByDescending(uhi => uhi.DateDisposed)
            .ToList();

        return View(recycledItems);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToHistory(int itemId)
    {
        // WM - getting the user id
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // WM - check if the user already has a UserHistory record, if not then make one
        var userHistory = await _context.UserHistories
            .FirstOrDefaultAsync(uh => uh.UserId == userId);

        if (userHistory == null)
        {
            userHistory = new UserHistory { UserId = userId };
            _context.UserHistories.Add(userHistory);
            await _context.SaveChangesAsync(); 
        }

        // WM - checking the item exists
        var originalItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == itemId);
        if (originalItem == null)
        {
            return NotFound();
        }

        // WM - creating a junction table record that links users to items
        var historyItem = new UserHistoryItem
        {
            ItemId = originalItem.ItemID,
            UserHistoryId = userHistory.UserHistoryId,
            DateDisposed = DateTime.UtcNow
        };

        _context.UserHistoryItems.Add(historyItem);
        await _context.SaveChangesAsync();

        // WM - redirect back to the history page to show the new item
        return RedirectToAction("Index", "History");
    }

    //WM - method to remove an item from a user's history
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromHistory(int itemId)
    {
        //WM - getting the user id
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // WM - getting the user's history
        var userHistory = await _context.UserHistories
            .FirstOrDefaultAsync(uh => uh.UserId == userId);

        //WM - if no history then return not found
        if (userHistory == null)
        {
            return NotFound();
        }

        // WM - finding the actual junction record that is connecting this user to this item
        var itemToDelete = await _context.UserHistoryItems
            .FirstOrDefaultAsync(uhi => uhi.ItemId == itemId && uhi.UserHistoryId == userHistory.UserHistoryId);

        // WM - if the junction record exists, delete it
        if (itemToDelete != null)
        {
            _context.UserHistoryItems.Remove(itemToDelete);
            await _context.SaveChangesAsync();
        }

        //WM - return and show the updated list
        return RedirectToAction(nameof(Index));
    }
}