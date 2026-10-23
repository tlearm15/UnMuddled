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
    //sisplaying the user's recycled items
    public async Task<IActionResult> Index()
    {
        // get the currently logged-in user's ID so their recycling history can be pulled
        var userId = _userManager.GetUserId(User);

        //if they arent logged in, redirect to login
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // getting the user's product history with items categories and disposal locs
        var userHistory = await _context.ProductHistories
            .Include(ph => ph.Items)
                .ThenInclude(i => i.Category)
            .Include(ph => ph.Items)
                .ThenInclude(i => i.ItemDisposals)
                    .ThenInclude(id => id.DisposalLocation)
            .FirstOrDefaultAsync(ph => ph.UserID == userId);

        // display empty list if there's no history or items yet
        if (userHistory == null || userHistory.Items == null)
        {
            return View(new List<Item>());
        }

        // showing recycled items ordered by desc
        var recycledItems = userHistory.Items
            .OrderByDescending(i => i.DateAdded)
            .ToList();

        return View(recycledItems);
    }
}