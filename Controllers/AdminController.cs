using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GroupProject.Models;
using GroupProject.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        //Get instance of the database context
        private readonly UnMuddledDbContext db;
        private readonly UserManager<User> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public AdminController(UnMuddledDbContext context, UserManager<User> um, RoleManager<IdentityRole> rm)
        {
            db = context;
            userManager = um;
            roleManager = rm;
        }
        // GET: AdminController
        public ActionResult Index()
        {
            return View();
        }

        public IActionResult UserList()
        {
            //Grabs users into a list for display in view
            List<User> users = db.Users.ToList();
            return View(users);
        }

        // GET: AdminController/Details/5
        public async Task<IActionResult> UserDetails(string? id)
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
        public IActionResult CreateNewUser()
        {
            CreateStaffViewModel model =  new CreateStaffViewModel();

            return View(model);

        }

        // POST: AdminController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewUser(CreateStaffViewModel model)
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
                JoinedOn = model.JoinedOn,
                EmailConfirmed = true
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

        [HttpGet]
        public async Task<IActionResult> EditUserRole(string id)
        {
            //Gets the User attached to the ID -RW
            User? user = await userManager.FindByIdAsync(id);
            
            if(user == null)
            {
                return NotFound("User not found");
            }

            var model = new ChangeRoleViewModel
            {
                Id = user.Id,
                userRole = user.UserRole
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserRole(ChangeRoleViewModel model)
        {
            //Ensure the model is complete and that a role defined in the 
            //enum has been defined -RW
            if(!ModelState.IsValid || !Enum.IsDefined(model.userRole))
            {
                ModelState.AddModelError("UserRole","Invalid role selected");
                return View(model);
            }
            //Gets user attached to the ID -RW
            User? user = await userManager.FindByIdAsync(model.Id);
            //Checks if user populated -RW
            if(user == null)
            {
                return NotFound("User not found");
            }
            //Stores the selected enum val for use -RW
            UserRole selectedRole = model.userRole;
            //Parses the enum val as a string to be used in Identity -Rw
            string newRole = selectedRole.ToString();
            //Ensure the role actually exists in Identity -RW
            if(!await roleManager.RoleExistsAsync(newRole))
            {
                ModelState.AddModelError("UserRole", "Role does not exist");
                return View(model);
            }
            //Gets current user role -RW
            var currentRole = await userManager.GetRolesAsync(user);
            //Remove the user from the role -RW
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRole);

            //Checks if role removal succeeded -RW
            if(!removeResult.Succeeded)
            {
                foreach(var error in removeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
            //Add the new Role to the user -RW
            var addResult = await userManager.AddToRoleAsync(user, newRole);
            //Checks if adding the new Role Succeeded -RW
            if(!addResult.Succeeded)
            {
                foreach(var error in addResult.Errors)
                {
                    ModelState.AddModelError(string.Empty,error.Description);
                }

                return View(model);
            }
            //Assigns the newly selected role to the UserRole enum val -RW
            user.UserRole = selectedRole;
            //Upadtes the User -RW
            var updateResult = await userManager.UpdateAsync(user);
            //Checks if update was successful -RW
            if(!updateResult.Succeeded)
            {
                foreach(var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            return RedirectToAction("UserList","Admin");
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
        public async Task<IActionResult> DeleteUserConfirmed(string id)
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
