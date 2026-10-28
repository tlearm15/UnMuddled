using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GroupProject.Models;

namespace GroupProject.Controllers;

public class HomeController : Controller
{
    private readonly UnMuddledDbContext _context;

    public HomeController(UnMuddledDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? room)
    {
        //m loading items with category and images
        var itemsQuery = _context.Items
            .Include(i => i.Category)
            .Include(i => i.Images)
            .AsQueryable();

        //m setting default room tag to be living room if none is chosen
        string activeRoom = string.IsNullOrWhiteSpace(room) ? "Living Room" : room;

        //m removing spaces so roomtag enum parses correctly
        string cleanRoomName = activeRoom.Replace(" ", "");

        if (Enum.TryParse<RoomTag>(cleanRoomName, true, out var selectedRoomTag))
        {
            itemsQuery = itemsQuery.Where(i => i.RoomTag == selectedRoomTag);
        }

        //m sending active room string to view for highlighting
        ViewData["CurrentRoom"] = activeRoom;

        var items = await itemsQuery.ToListAsync();

        // m returtning items to view
        return View(items);
    }

    // jm: Symbols View
    public IActionResult Symbols()
    {
        
        return View();
    }
}