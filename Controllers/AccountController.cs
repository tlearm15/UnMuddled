using System.Text;
using GroupProject.Models;
using GroupProject.Services;
using GroupProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Controllers;

public class AccountController : Controller
{
    private readonly UnMuddledDbContext _context;
    private readonly IStorageService _storageService;
    private readonly IEmailSender<User> emailSender;
    private readonly EmailService emailService;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly SignInManager<User> signInManager;
    private readonly UserManager<User> userManager;

    public AccountController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmailSender<User> emailSender,
        EmailService emailService,
        UnMuddledDbContext context,
        IStorageService storageService
    )
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.emailSender = emailSender;
        this.emailService = emailService;
        _context = context;
        _storageService = storageService;
    }

    /// <summary>
    /// GET
    /// TQ- Stub for viewing profile
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Member, Staff, Admin")]
    public async Task<IActionResult> Index()
    {
        //m getting the currently logged in user from identity manager
        var user = await userManager.GetUserAsync(User);
        //m if user is null yhen return not found
        if (user is null) return NotFound();

        var userHistory = await _context.UserHistories
            .Include(uh => uh.UserHistoryItems).ThenInclude(uhi => uhi.Item).ThenInclude(i => i.Category)
            .Include(uh => uh.UserHistoryItems).ThenInclude(uhi => uhi.Item).ThenInclude(i => i.Images)
            .FirstOrDefaultAsync(uh => uh.UserId == user.Id);

        //m getting recycled items
        //wm - updating this to order by descending so that it displays most recently recycled
        var recycledItems = userHistory?.UserHistoryItems?
            .Where(uhi => uhi.Item != null)
            .OrderByDescending(uhi => uhi.DateDisposed)
            .Select(uhi => uhi.Item!)
            .ToList() ?? new List<Item>();

        //m passing the recycled items to the view
        ViewData["RecycledItems"] = recycledItems;

        return View(user);
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LogInViewModel model)
    {
        //Uses login viewmodel to validate the login form and then uses the SignInManager to sign in the user if the credentials are valid - RW
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result =
            await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe,
                lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
        }

        // TQ- Credentials correct but account has 2FA enabled, so redirect to the verification code page
        if (result.RequiresTwoFactor)
            return RedirectToAction("TwoFactorAuth", "Account");
        // TQ- user locked out
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked out. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
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

        var member = new User //Builds new Member User -RW
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
        //Attempts to create the Member -RW
        var result = await userManager.CreateAsync(member, model.Password);
        if (result.Succeeded)
        {
            // Assign the default role to the user -RW
            await userManager.AddToRoleAsync(member, "Member");
            //await signInManager.SignInAsync(user, isPersistent: false);
            await SendAccountConfirm(member); // Send account confirmation email -RW

            //TL - Sets success popup message for the login page view
            TempData["SuccessMessage"] = "Email Sent! Please check inbox for confirmation";
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

        //Grabs details to populate the model -RW
        var model = new ChangeDetailsViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            AddressLine1 = user.AddressLine1,
            AddressLine2 = user.AddressLine2,
            City = user.City,
            PostCode = user.PostCode,
            Is2FAEnabled = user.TwoFactorEnabled, // TQ- match it up to IdentityUser 2fa enabled
            ProfilePictureUrl = user.ProfilePictureUrl //TL - get users current profile picture to display
        };
        ViewBag.MaskedEmail = MaskEmail(user.Email ?? string.Empty);

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeDetails(ChangeDetailsViewModel model)
    {
        // Uses the ChangeDetailsViewModel to validate the change details form
        // then uses the UserManager to update the user's details if the credentials are valid - RW
        //Gets the logged in user and assigns it to a var -RW
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("User not found");
        }

        if (!ModelState.IsValid)
        {
            // TQ- shows where 2FA codes would be sent on the redisplayed page
            ViewBag.MaskedEmail = MaskEmail(user.Email ?? string.Empty);
            return View(model);
        }

        // Handle Profile Picture Upload if provided
        if (model.RemoveProfilePicture)
        {
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                try
                {
                    string extension = Path.GetExtension(user.ProfilePictureUrl);
                    string customFileName = $"profile_{user.Id}{extension}";

                    await _storageService.DeleteFileAsync("profile", customFileName);
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "An error occurred while removing your profile picture.";
                }
            }

            user.ProfilePictureUrl = null;
        }

        else if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            try
            {
                string extension = Path.GetExtension(model.ProfilePicture.FileName);
                string customFileName = $"profile_{user.Id}{extension}";

                string imageUrl =
                    await _storageService.UploadFileAsync(model.ProfilePicture, "profile", customFileName);
                user.ProfilePictureUrl = imageUrl;
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Failed to upload profile picture.");
                ViewBag.MaskedEmail = MaskEmail(user.Email ?? string.Empty);
                return View(model);
            }
        }

        //Overwrites the user properties with what is in the model -RW
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.AddressLine1 = model.AddressLine1;
        user.AddressLine2 = model.AddressLine2;
        user.City = model.City;
        user.PostCode = model.PostCode;
        //Attempts to update the User -RW
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            // TQ- Sync the 2FA toggle with Identity's built-in TwoFactorEnabled flag.
            //    SetTwoFactorEnabledAsync also rotates the security stamp so it takes effect immediately
            if (user.TwoFactorEnabled == model.Is2FAEnabled)
                return RedirectToAction("Index", "Account");
            var twoFactorResult = await userManager.SetTwoFactorEnabledAsync(user, model.Is2FAEnabled);
            if (twoFactorResult.Succeeded)
                return RedirectToAction("Index", "Account");
            // TQ- shows where 2FA codes would be sent on the redisplayed page
            ViewBag.MaskedEmail = MaskEmail(user.Email ?? string.Empty);
            foreach (var error in twoFactorResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // TQ- shows where 2FA codes would be sent on the redisplayed page
        ViewBag.MaskedEmail = MaskEmail(user.Email ?? string.Empty);
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [Authorize]
    [HttpGet]
    public IActionResult DeleteAccount()
    {
        return View(new DeleteAccountViewModel());
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
        if (!passwordValid)
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

            return View(model);
        }
    }

    public async Task SendAccountConfirm(User user)
    {
        // This method is used to send an account confirmation email,
        // to the user after registration. -RW

        //Create Token for Confirmation -RW
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        //TL - Encodes token for safe usage in URL
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        //Url for the confirmation link, which will be sent to the user's email. -RW
        var confirmationLink = Url.Action("ConfirmEmail", "Account",
            new { userId = user.Id, token = encodedToken }, Request.Scheme);

        //TL - Sends confirmation email
        await emailSender.SendConfirmationLinkAsync(user, user.Email ?? string.Empty, confirmationLink ?? string.Empty);
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

        //TL - Decodes the token for use in this method
        var decodedTokenBytes = WebEncoders.Base64UrlDecode(token);
        var normalToken = Encoding.UTF8.GetString(decodedTokenBytes);

        // Confirm the user's email using the provided token -RW
        var result = await userManager.ConfirmEmailAsync(user, normalToken);

        if (result.Succeeded)
        {
            //TL - Redirects the user to the login page with a popup message
            TempData["SuccessMessage"] = "Email successfully confirmed! You can now log in.";
            return RedirectToAction("Login", "Account");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        //TL - Redirects the user to the login page with a popup message
        TempData["SuccessMessage"] = "Email confirmation failed.";
        return RedirectToAction("Login", "Account");
    }

    /// <summary>
    /// TQ - Step 1 of 2FA login: find the pending user (stored in Identity's two-factor cookie by PasswordSignInAsync),
    /// generate a 6-digit code and email it
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> TwoFactorAuth()
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return RedirectToAction("Login", "Account");
        //Generates and emails the one-time code (only works for users with 2FA enabled)
        await SendTwoFactorCode(user);

        var model = new TwoFAViewModel
        {
            MaskedEmail = MaskEmail(user.Email ?? string.Empty)
        };

        return View(model);
    }

    /// <summary>
    /// TQ - Step 2 of 2FA login: validate the code the user entered
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TwoFactorAuth(TwoFAViewModel model)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            model.MaskedEmail = MaskEmail(user.Email ?? string.Empty);
            return View(model);
        }

        //Validates the code. Identity keeps the RememberMe choice from the login page,
        //RememberDevice stops 2FA being required again on this browser
        var result = await signInManager.TwoFactorSignInAsync("Email", model.Code,
            isPersistent: model.RememberDevice, rememberClient: model.RememberDevice);

        if (result.Succeeded)
            return RedirectToAction("Index", "Account");

        if (result.IsLockedOut)
        {
            //Failed code attempts count towards Identity's account lockout
            ModelState.AddModelError(string.Empty, "Too many invalid codes entered. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid verification code.");
        return View(model);
    }

    /// <summary>
    /// TQ- Lets the user request a fresh code if theirs expired or the email was lost
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> TwoFactorResend()
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return RedirectToAction("Login", "Account");

        await SendTwoFactorCode(user);
        TempData["SuccessMessage"] = "A new verification code has been sent to your email.";

        return RedirectToAction("TwoFactorAuth", "Account");
    }

    /// <summary>
    /// TQ- Generates a two-factor token for the pending user and emails it
    /// </summary>
    /// <param name="user"></param>
    private async Task SendTwoFactorCode(User user)
    {
        var code = await userManager.GenerateTwoFactorTokenAsync(user, "Email");
        await emailService.SendTwoFactorCodeAsync(user, user.Email ?? string.Empty, code);
    }

    /// <summary>
    /// TQ- Hides the user's email on the code entry page, e.g. j***h@gmail.com
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex < 2 ? "your email" : $"{email[0]}***{email[atIndex - 1]}@{email[(atIndex + 1)..]}";
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
    {
        if (profilePicture == null || profilePicture.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a valid image file.";
            return RedirectToAction(nameof(Index));
        }

        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("User not found");
        }

        try
        {
            // Unique naming convention for profile pictures using User ID
            string extension = Path.GetExtension(profilePicture.FileName);
            string customFileName = $"profile_{user.Id}{extension}";

            // Upload to the "profile" folder in Oracle Object Storage
            string imageUrl = await _storageService.UploadFileAsync(profilePicture, "profile", customFileName);

            user.ProfilePictureUrl = imageUrl;
            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile picture updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile picture in database.";
            }
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "An error occurred while uploading your profile picture.";
        }

        return RedirectToAction(nameof(Index));
    }
}