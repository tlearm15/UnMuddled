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

    // initializing the controller with the database context and user manager
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

        //WM - if the user's trying to view a history that isn't their own and they aren't an admin, forbid the access
        if (userId != _userManager.GetUserId(User) && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            return Forbid();
        }

        // WM - getting the user's product history with items categories and disposal locs
        var userHistory = await _context.ProductHistories
            .Include(ph => ph.Items)
                .ThenInclude(i => i.Category)
            .Include(ph => ph.Items)
                .ThenInclude(i => i.ItemDisposals)
                    .ThenInclude(id => id.DisposalLocation)
            .FirstOrDefaultAsync(ph => ph.UserID == userId);

        //WM - display empty list if there's no history or items yet
        if (userHistory == null || userHistory.Items == null)
        {
            return View(new List<Item>());
        }

        // WM - showing recycled items ordered by desc
        var recycledItems = userHistory.Items
            .OrderByDescending(i => i.DateAdded)
            .ToList();

        return View(recycledItems);
    }
}