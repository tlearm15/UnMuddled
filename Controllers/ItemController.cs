using GroupProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Controllers;

[Authorize(Roles = "Admin, Staff")]
public class ItemController : Controller
{
    private readonly UnMuddledDbContext _context;

    public ItemController(UnMuddledDbContext context)
    {
        _context = context;
    }

    //GETS THE ITEMS AND FILTERS THEM BASED ON THE SEARCH TERM, CATEGORY, ROOM TAG, AND RECYCLABLE STATUS
    public async Task<IActionResult> Index(string? searchTerm, int? categoryId, RoomTag? roomTag, bool? isRecyclable)
    {
        var items = _context.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            items = items.Where(i => i.ItemName.Contains(term) || i.Description.Contains(term));
        }

        if (categoryId.HasValue)
        {
            items = items.Where(i => i.CategoryID == categoryId.Value);
        }

        if (roomTag.HasValue)
        {
            items = items.Where(i => i.RoomTag == roomTag.Value);
        }

        if (isRecyclable.HasValue)
        {
            items = items.Where(i => i.IsRecyclable == isRecyclable.Value);
        }

        var itemList = await items
            .OrderBy(i => i.ItemName)
            .ToListAsync();

        ViewBag.Categories = new SelectList(
            await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(),
            nameof(Category.CategoryID),
            nameof(Category.CategoryName),
            categoryId);

        ViewBag.SearchTerm = searchTerm;
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.SelectedRoomTag = roomTag;
        ViewBag.SelectedIsRecyclable = isRecyclable;

        return View(itemList);
    }

    // GETS THE DETAILS OF A SPECIFIC ITEM BASED ON ITS ID
    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.Items
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.ItemID == id);

        if (item is null)
        {
            return NotFound();
        }

        return View(item);
    }

    // CREATES A NEW ITEM AND POPULATES THE CATEGORY DROPDOWN LIST
    public IActionResult Create()
    {
        ViewBag.Categories = new SelectList(
            _context.Categories.OrderBy(c => c.CategoryName).ToList(),
            nameof(Category.CategoryID),
            nameof(Category.CategoryName));

        return View();
    }
    // CREATES A NEW ITEM AND SAVES IT TO THE DATABASE IF THE MODEL STATE IS VALID, OTHERWISE RETURNS THE VIEW WITH THE CATEGORY DROPDOWN LIST POPULATED
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Item item)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(),
                nameof(Category.CategoryID),
                nameof(Category.CategoryName),
                item.CategoryID);

            return View(item);
        }

        _context.Add(item);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // CREATES AN EDIT VIEW FOR A SPECIFIC ITEM BASED ON ITS ID AND POPULATES THE CATEGORY DROPDOWN LIST
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        ViewBag.Categories = new SelectList(
            await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(),
            nameof(Category.CategoryID),
            nameof(Category.CategoryName),
            item.CategoryID);

        return View(item);
    }

    // UPDATES A SPECIFIC ITEM BASED ON ITS ID AND THE PROVIDED ITEM OBJECT, SAVES CHANGES TO THE DATABASE IF THE MODEL STATE IS VALID, OTHERWISE RETURNS THE VIEW WITH THE CATEGORY DROPDOWN LIST POPULATED
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Item item)
    {
        if (id != item.ItemID)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(),
                nameof(Category.CategoryID),
                nameof(Category.CategoryName),
                item.CategoryID);

            return View(item);
        }

        try
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Items.AnyAsync(e => e.ItemID == id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // DELETES A SPECIFIC ITEM BASED ON ITS ID AND CONFIRMS THE DELETION, REMOVING IT FROM THE DATABASE
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Items
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.ItemID == id);

        if (item is null)
        {
            return NotFound();
        }

        return View(item);
    }

    // CONFIRMS THE DELETION OF A SPECIFIC ITEM BASED ON ITS ID AND REMOVES IT FROM THE DATABASE
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
