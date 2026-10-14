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
            //Check if EF Core can successfully reach the database
            var canConnect = await _context.Database.CanConnectAsync();

            if (canConnect)
            {
  
                var userCount = await _context.Users.CountAsync();
                return Ok(new { Success = true, Message = "Successfully connected to PostgreSQL via mTLS!", UserCount = userCount });
            }
            else
            {
                return StatusCode(500, new { Success = false, Message = "Could not connect to the database." });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Error = ex.Message, InnerError = ex.InnerException?.Message });
        }
    }
}