using Microsoft.AspNetCore.Mvc;
using GroupProject.Models;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Controllers;

public class HomeController : Controller
{
    private readonly UnMuddledDbContext _context;
    
    public HomeController(UnMuddledDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    //TO BE REMOVED
    [HttpGet("test-db")]
    public async Task<IActionResult> TestDb()
    {
        try
        {
            //test if can connect to db
            bool canConnect = await _context.Database.CanConnectAsync();

            //if connection successful
            if (canConnect)
            {
                //success
                return Json(new { success = true, message = "Database connection is successful!" });
            }
            else
            {
                //connection made but other error
                return Json(new { success = false, message = "Database connection returned false (server reachable, but refused connection)." });
            }
        }
        //connection unsuccessful 
        catch (Exception ex)
        {
            return Json(new { 
                //print error message
                success = false, 
                message = "Database connection failed.", 
                error = ex.Message 
            });
        }
    }
}