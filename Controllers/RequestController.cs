using GroupProject.Models;
using GroupProject.Services;
using GroupProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GroupProject.Controllers;

[Authorize]
public class RequestController(UnMuddledDbContext dbCtx, IStorageService storageService) : Controller
{
    private void PopulateCategories(int? selected = null, RoomTag? selectedRoom = null)
    {
        ViewBag.Categories = new SelectList(
            dbCtx.Categories.OrderBy(c => c.CategoryName).ToList(),
            nameof(Category.CategoryID),
            nameof(Category.CategoryName),
            selected);
        
        ViewBag.RoomTags = Enum.GetValues<RoomTag>()
            .Select(r => new SelectListItem
            {
                Value = r.ToString(),
                Text = r.ToString(),
                Selected = selectedRoom.HasValue && r == selectedRoom.Value
            }).ToList();
    }

    [HttpGet]
    public IActionResult Index()
    {
        PopulateCategories();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Request request, List<IFormFile> imageFiles)
    {
        if (!ModelState.IsValid)
        {
            PopulateCategories(request.CategoryID, request.RoomTag);
            return View(request);
        }

        request.UserID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        request.Status = RequestStatus.Pending;
        request.SubmittedOn = DateTime.UtcNow;
        request.ItemID = null;

        // Save request first to generate RequestID in the database
        dbCtx.Requests.Add(request);
        await dbCtx.SaveChangesAsync();

        // Handle multi-image upload to Oracle Object Storage
        if (imageFiles != null && imageFiles.Count > 0)
        {
            bool isFirst = true;
            foreach (var file in imageFiles)
            {
                if (file.Length > 0)
                {
                    string imageUrl = await storageService.UploadFileAsync(file, "requests");

                    dbCtx.Images.Add(new Image
                    {
                        ImageURL = imageUrl,
                        IsPrimary = isFirst, // First image is marked primary optionally
                        RequestID = request.RequestID
                    });

                    isFirst = false;
                }
            }
            await dbCtx.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Staff, Admin")]
    public IActionResult ListRequests()
    {
        var requests = dbCtx.Requests
            .Include(r => r.User)
            .Include(r => r.Category)
            .Include(r => r.Images)
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.SubmittedOn)
            .ToList();
        
        return View("List", requests);
    }

    [Authorize(Roles = "Staff, Admin")]
    public IActionResult ViewRequest(int id)
    {
        var request = dbCtx.Requests
            .Include(r => r.User)
            .Include(r => r.Category)
            .Include(r => r.Images)
            .FirstOrDefault(r => r.RequestID == id);

        if (request is null)
        {
            return NotFound();
        }

        return View("Details", request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff, Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var request = dbCtx.Requests
            .Include(r => r.Images)
            .FirstOrDefault(r => r.RequestID == id);

        if (request is null)
        {
            return NotFound();
        }

        // Convert approved request into a live Item
        var newItem = new Item
        {
            ItemName = request.ItemName,
            CategoryID = request.CategoryID,
            Description = "Created from approved user request.",
            RoomTag = request.RoomTag,
            DateAdded = DateTime.UtcNow
        };

        dbCtx.Items.Add(newItem);
        await dbCtx.SaveChangesAsync(); // Generates newItem.ItemID

        // Reassign uploaded images from request over to the new item
        if (request.Images != null && request.Images.Any())
        {
            foreach (var image in request.Images)
            {
                image.ItemID = newItem.ItemID;
                image.RequestID = null;
            }
        }

        request.Status = RequestStatus.Approved;
        request.ItemID = newItem.ItemID;

        await dbCtx.SaveChangesAsync();
        
        return RedirectToAction(nameof(ViewRequest), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff, Admin")]
    public async Task<IActionResult> Reject(int id)
    {
        var request = dbCtx.Requests.Find(id);
        if (request is null)
        {
            return NotFound();
        }
        
        request.Status = RequestStatus.Rejected;
        await dbCtx.SaveChangesAsync();
        
        return RedirectToAction(nameof(ViewRequest), new { id });
    }
}