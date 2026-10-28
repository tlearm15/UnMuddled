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
        public ActionResult CreateNewUser()
        {
            CreateStaffViewModel model =  new CreateStaffViewModel();

            return View(model);

        }

        // POST: AdminController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateNewUser(CreateStaffViewModel model)
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

            if(!ModelState.IsValid)
            {
                return View(model);
            }

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

            foreach(var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }


        // GET: AdminController/Delete/5
        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            //Checks if id has value -RW
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("User not found");
            }
            //Finds use attached to id -RW
            var user = await userManager.FindByIdAsync(id);
            //Checks user has a value -RW
            if (user == null)
            {
                return NotFound("User not found");
            }
            //Checks if id is loggedin users id
            if (id == userManager.GetUserId(User))
            {
                return RedirectToAction("UserList");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("DeleteUser")]
        public async Task<ActionResult> DeleteUserConfirmed(string id)
        {
            //Check that id has a value -RW
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("User not found");
            }
            //Ensure it is not own id -RW
            if (id == userManager.GetUserId(User))
            {
                return RedirectToAction("UserList");
            }
            //Finds user attached to id -RW
            User? user = await userManager.FindByIdAsync(id);
            //Checks user has value -RW
            if (user == null)
            {
                return NotFound("User not found");
            }
            //Attempts to Delete User -RW
            var result = await userManager.DeleteAsync(user);
            //If Deletion fail. Return to view -RW
            if (!result.Succeeded)
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View("DeleteUser", user);
            }
            //Deletion success. Returns to User List page
            return RedirectToAction("UserList", "Admin");
            // POST: AdminController/Delete/5

        } 
    }
}
