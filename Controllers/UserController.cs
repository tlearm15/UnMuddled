using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GroupProject.Models;

public class UserController(UserManager<User> userManager) : Controller
{
    /// <summary>
    /// GET
    /// TQ- Stub for viewing profile
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "User, Staff, Admin")]
    public IActionResult Index()
    {
        return View();
    }
    /// <summary>
    /// GET
    /// TQ - user manager for admins
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    public IActionResult ManageUsers()
    {
        var users = userManager.Users.ToList();
        var userRoles = (from user in users let roles = userManager.GetRolesAsync(user).Result select (user, roles)).ToList();

        return View(userRoles);
    }
    /// <summary>
    /// POST
    /// TQ- change user roles, which only Admins can do
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult ChangeRole(string userId, string role)
    {
        var user = userManager.FindByIdAsync(userId).Result;
        if (user is null)
            return NotFound();

        var currentRoles = userManager.GetRolesAsync(user).Result;
        userManager.RemoveFromRolesAsync(user, currentRoles).Wait();
        userManager.AddToRoleAsync(user, role).Wait();

        return RedirectToAction(nameof(ManageUsers));
    }
    /// <summary>
    /// GET
    /// TQ- Creates a new User
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    public IActionResult CreateUser()
    {
        ViewData["Roles"] = new List<string> { "Admin", "Staff", "User" };
        return View();
    }
    /// <summary>
    ///  POST
    /// TQ- Creates a new User
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="email"></param>
    /// <param name="phoneNumber"></param>
    /// <param name="address"></param>
    /// <param name="postcode"></param>
    /// <param name="password"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult CreateUser(string userName, string email, string phoneNumber, string address, string postcode, string password, string role)
    {
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)) return View();
        
        ModelState.AddModelError( "", "Email and password are required." );
        ViewData["Roles"] = new List<string> { "Admin", "Staff", "User" };
        // todo: process the input from the form 
        return View();
    }
    /// <summary>
    /// GET
    /// TQ- Edits a user's details
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    public IActionResult EditUser(string id)
    {
        var user = userManager.FindByIdAsync(id).Result;
        if( user is null )
            return NotFound();

        var roles = userManager.GetRolesAsync(user).Result;
        ViewData["CurrentRole"] = roles.FirstOrDefault() ?? "";
        ViewData["Roles"] = new List<string> { "Admin", "Staff", "User" };

        return View(user);
    }
    /// <summary>
    /// GET
    /// Assigns a User (accepts their Id) a role
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    public IActionResult AssignRole(string id)
    {
        var user = userManager.FindByIdAsync(id).Result;
        if (user is null)
            return NotFound();

        var roles = userManager.GetRolesAsync(user).Result;
        ViewData["CurrentRole"] = roles.FirstOrDefault() ?? "";
        ViewData["Roles"] = new List<string> { "Admin", "OfficeClerk", "Customer" };

        return View(user);
    }
    /// <summary>
    /// POST
    /// Assigns a User (accepts their Id) a role
    /// </summary>
    /// <param name="id"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult AssignRole(string id, string role)
    {
        var user = userManager.FindByIdAsync(id).Result;
        if (user is null)
            return NotFound();

        var currentRoles = userManager.GetRolesAsync(user).Result;
        userManager.RemoveFromRolesAsync(user, currentRoles).Wait();
        userManager.AddToRoleAsync(user, role).Wait();

        //TQ- todo: there should be a redirect to the user's page showing their role
        return View();
    }
    /// <summary>
    /// GET
    /// TQ- Terminates a User's account
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    public IActionResult Ban(string id)
    {
        var user = userManager.FindByIdAsync(id).Result;
        if (user is null)
            return NotFound();

        return View(user);
    }
    /// <summary>
    /// POST
    /// TQ- Terminates a User's account
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ActionName("Ban")]
    public IActionResult BanConfirmed(string id)
    {
        var user = userManager.FindByIdAsync(id).Result;
        if (user is null)
            return NotFound();

        var adminUsers = userManager.GetUsersInRoleAsync("Admin").Result;
        if (adminUsers.Count <= 1 && adminUsers.Any(u => u.Id == id))
            //TQ- todo: there should be a redirect to the user's page showing their role
            return View();

        userManager.DeleteAsync(user).Wait();
        
        //TQ- todo: there should be a redirect to the user's page showing their role
        return View();
    }
}