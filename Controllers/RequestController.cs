using System.Security.Claims;
using GroupProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GroupProject.Controllers;

[Authorize]
public class RequestController(UnMuddledDbContext dbCtx) : Controller
{
    /// <summary>
    /// TQ- Fills
    /// </summary>
    /// <param name="selected"></param>
    private void PopulateCategories(int? selected = null)
    {
        ViewBag.Categories = new SelectList(
            dbCtx.Categories.OrderBy(c => c.CategoryName).ToList(),
            nameof(Category.CategoryID),
            nameof(Category.CategoryName),
            selected);
    }
    /// <summary>
    /// GET
    /// TQ- This will be the item submission form
    /// todo: might reuse this exact view but have frontend validation for Staff/Admin to see prefilled form
    /// </summary>
    /// <returns></returns>
    public IActionResult Index()
    {
        return View();
    }
    /// <summary>
    /// POST
    /// TQ- Item submission form data coming back to us
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(Request request)
    {
        if( !ModelState.IsValid )
        {
            PopulateCategories( request.CategoryID );
            return View( request );
        }
        request.UserID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        request.Status = RequestStatus.Pending;
        request.SubmittedOn = DateTime.UtcNow;
        request.ItemID = null;
        // shove this into the DB so a Staff can check it later
        dbCtx.Requests.Add( request );
        dbCtx.SaveChanges();
        // Return to this view
        return RedirectToAction( nameof(Index) );
    }
    /// <summary>
    /// GET
    /// TQ- todo: show some page to view all the requests made that only Staff/Admins can see
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Staff, Admin")]
    public IActionResult ListRequests()
    {
        return View();
    }
    /// <summary>
    /// GET
    /// TQ- stub
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Staff, Admin")]
    public IActionResult ViewRequest()
    {
        //todo: prefill submission form with what was already requested - view will show the buttons to accept/reject
        return RedirectToAction( nameof( Index ) );
    }
}