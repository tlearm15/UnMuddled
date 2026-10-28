using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GroupProject.Models;
using GroupProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace GroupProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LogInViewModel model)
        {
            //Uses login viewmodel to validate the login form and then uses the SignInManager to sign in the user if the credentials are valid - RW
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            //Uses register viewmodel to validate the registration form and then uses the UserManager to create a new user if the credentials are valid - RW
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var member = new User
            {
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                PostCode = model.PostCode,
                UserRole = UserRole.Member,
                JoinedOn = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(member, model.Password);
            if (result.Succeeded)
            {
                // Assign the default role to the user -RW
                await userManager.AddToRoleAsync(member, "Member");
                //await signInManager.SignInAsync(user, isPersistent: false);
                await SendAccountConfirm(member); // Send account confirmation email -RW
                return RedirectToAction("Login", "Account");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Uses the SignInManager to sign out the user and redirect to the home page - RW
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            // Uses the ChangePasswordViewModel to validate the change password form
            // then uses the UserManager to change the user's password if the credentials are valid - RW
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Get the currently logged-in user -RW
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }
            // Check if the new password and confirm new password match -RW
            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                // Sign the user out after changing the password.
                // This is a security measure to ensure that the user re-authenticates with the new password. -RW
                await signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangeDetails()
        {
            // Uses the ChangeDetailsViewModel to populate the change details form with the current user's details - RW
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }
            var model = new ChangeDetailsViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                AddressLine1 = user.AddressLine1,
                AddressLine2 = user.AddressLine2,
                City = user.City,
                PostCode = user.PostCode
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeDetails(ChangeDetailsViewModel model)
        {
            // Uses the ChangeDetailsViewModel to validate the change details form
            // then uses the UserManager to update the user's details if the credentials are valid - RW
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.AddressLine1 = model.AddressLine1;
            user.AddressLine2 = model.AddressLine2;
            user.City = model.City;
            user.PostCode = model.PostCode;
            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeleteAccount()
        {
            // This method is used to display the delete account confirmation page.
            // It retrieves the currently logged-in user and passes it to the view. -RW
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }
            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("DeleteAccount")]
        public async Task<IActionResult> DeleteAccountConfirmed(DeleteAccountViewModel model)
        {
            // This method is used to delete the currently logged-in user's account.
            // It retrieves the user, deletes it using the UserManager, and then signs the user out. -RW
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }
            // Check if the provided password is correct before deleting the account -Rw
            var passwordValid = await userManager.CheckPasswordAsync(user, model.Password);
            if(!passwordValid)
            {
                ModelState.AddModelError(string.Empty, "Incorrect password. Please try again.");
                return View(model);
            }

            // Delete the user account -RW     
            var result = await userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                await signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(user);
            }
        }

        public async Task SendAccountConfirm(User user)
        {
            // This method is used to send an account confirmation email,
            // to the user after registration. -RW

            //Create Token for Confirmation -RW
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            //Url for the confirmation link, which will be sent to the user's email. -RW
            var confirmationLink = Url.Action("ConfirmEmail", "Account",
                new { userId = user.Id, token = token }, Request.Scheme);

            // Here you would send the email using your email service. -RW
            //await emailService.SendVerificationEmailAsync(user.Email, confirmationLink);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            //This method is used to confirm the user's email address after registration.
            //It takes the userId and token as parameters, which are sent in the confirmation email. - RW
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest("Invalid Link"); 
            }
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }
            // Confirm the user's email using the provided token -RW
            var result = await userManager.ConfirmEmailAsync(user, token);

            if(result.Succeeded)
            {
                return View("ConfirmEmail"); //Or whatever view you want to show after successful confirmation -RW
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View("Error"); //Or whatever view you want to show in case of an error -RW
        }
    }
}
