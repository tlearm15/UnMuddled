using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GroupProject.Models;
using GroupProject.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;

namespace GroupProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        //Get instance of the database context
        private readonly UnMuddledDbContext db;
        private readonly UserManager<User> userManager;
        // GET: AdminController
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult UserList()
        {
            //Grabs users into a list for display in view
            List<User> users = db.Users.ToList();
            return View(users);
        }

        // GET: AdminController/Details/5
        public async Task<ActionResult> UserDetails(string? id)
        {
            //Find user by ID and display details in view - RW
            if (id == null)
            {
                return NotFound("User not found");
            }
            //Find user by ID
            User? user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }
            
            return View(user);

        }

        // GET: AdminController/Create
        [HttpGet]
        public async Task<ActionResult> CreateNewUser()
        {
            CreateStaffViewModel model = new CreateStaffViewModel();
            
            return View(model);
            
        }

        // POST: AdminController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task <ActionResult> CreateNewUser(CreateStaffViewModel model)
        {
            if (!Enum.IsDefined(model.UserRole))
            {
                ModelState.AddModelError("UserRole", "Invalid Role Selected");
                return View(model);
            }

            UserRole selectedRole = model.UserRole;

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                PostCode = model.PostCode,
                UserRole = selectedRole,
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(user, selectedRole.ToString());
                if (roleResult.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: AdminController/Edit/5
        public ActionResult EditUser(int id)
        {
            return View();
        }

        // POST: AdminController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AdminController/Delete/5
        public ActionResult DeleteUser(int id)
        {
            return View();
        }

        // POST: AdminController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
