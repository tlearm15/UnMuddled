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

        // WM - getting the user's product history with items categories, images, and disposal locs
        var userHistory = await _context.ProductHistories
            .Include(ph => ph.Items)
                .ThenInclude(i => i.Category)
            .Include(ph => ph.Items)
                .ThenInclude(i => i.Images)
            .Include(ph => ph.Items)
                .ThenInclude(i => i.ItemDisposals)
                    .ThenInclude(id => id.DisposalLocation)
            .FirstOrDefaultAsync(ph => ph.UserID == userId);

        //WM - display empty list if there's no history or items yet
        if (userHistory == null || userHistory.Items == null)
        {
            return View(new List<Item>());
        }

        // WM - getting recycled items ordered by desc
        var recycledItems = userHistory.Items
            .OrderByDescending(i => i.DateAdded)
            .ToList();

        return View(recycledItems);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToHistory(int itemId)
    {
        //WM - getting the user id
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // WM - check if the user already has product history record if not then make one
        var userHistory = await _context.ProductHistories
            .FirstOrDefaultAsync(ph => ph.UserID == userId);

        if (userHistory == null)
        {
            userHistory = new ProductHistory { UserID = userId };
            _context.ProductHistories.Add(userHistory);
            await _context.SaveChangesAsync(); // Save to generate the HistoryID
        }

        //WM - get the original item so i can clone it to the user's history
        var originalItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == itemId);
        if (originalItem == null)
        {
            return NotFound();
        }

        // WM - creating a new item for the user's history that's based on the original item
        var recycledItem = new Item
        {
            ItemName = originalItem.ItemName,
            Description = originalItem.Description,
            IsRecyclable = originalItem.IsRecyclable,
            CategoryID = originalItem.CategoryID,
            RoomTag = originalItem.RoomTag,
            HistoryID = userHistory.HistoryID,
            DateAdded = DateTime.UtcNow
        };

        _context.Items.Add(recycledItem);
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

        // WM - getting the user's recycling history and making sure it belongs to them
        var userHistory = await _context.ProductHistories
            .FirstOrDefaultAsync(ph => ph.UserID == userId);

        //WM - if no item then return not found
        if (userHistory == null)
        {
            return NotFound();
        }

        // WM - getting the item to delete it and making sure its on the user's history
        var itemToDelete = await _context.Items
            .FirstOrDefaultAsync(i => i.ItemID == itemId && i.HistoryID == userHistory.HistoryID);

        //WM - if the item exists then delete it and return to the history page
        if (itemToDelete != null)
        {
            _context.Items.Remove(itemToDelete);
            await _context.SaveChangesAsync();
        }
        //WM - reutnr and show the updated list
        return RedirectToAction(nameof(Index));
    }
}