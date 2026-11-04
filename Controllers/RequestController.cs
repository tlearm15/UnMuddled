using System.Security.Claims;
using GroupProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
    public IActionResult Index() => View();
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
    /// TQ- show all the requests made, only Staff/Admins can see
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Staff, Admin")]
    public IActionResult ListRequests()
    {
        var requests = dbCtx.Requests
            .Include( r => r.User )
            .Include( r => r.Category )
            .OrderBy( r => r.Status )
            .ThenByDescending( r => r.SubmittedOn )
            .ToList();
        
        return View( "List", requests );
    }
    /// <summary>
    /// GET
    /// TQ- details of a single request with buttons to accept/reject
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize(Roles = "Staff, Admin")]
    public IActionResult ViewRequest(int id)
    {
        var request = dbCtx.Requests
            .Include( r => r.User )
            .Include( r => r.Category )
            .FirstOrDefault( r => r.RequestID == id );
        
        if( request is null )
            return NotFound();
        
        return View( "Details", request );
    }
    /// <summary>
    /// POST
    /// TQ- Staff/Admin marks a request as Approved
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff, Admin")]
    public IActionResult Approve(int id)
    {
        var request = dbCtx.Requests.Find( id );
        if( request is null )
            return NotFound();

        request.Status = RequestStatus.Approved;
        dbCtx.SaveChanges();
        
        return RedirectToAction( nameof( ViewRequest ), new { id } );
    }
    /// <summary>
    /// POST
    /// TQ- Staff/Admin marks a request as Rejected
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff, Admin")]
    public IActionResult Reject(int id)
    {
        var request = dbCtx.Requests.Find( id );
        if( request is null )
            return NotFound();
        
        request.Status = RequestStatus.Rejected;
        dbCtx.SaveChanges();
        
        return RedirectToAction( nameof( ViewRequest ), new { id } );
    }
}