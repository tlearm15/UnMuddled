using System.ComponentModel.DataAnnotations;
namespace GroupProject.ViewModels;

public class TwoFAViewModel
{
    [Required(ErrorMessage = "Please enter the verification code sent to your email.")]
    [Display(Name = "Verification code")]
    public string Code { get; set; } = string.Empty;
    //Browser will skip 2FA on future logins for a while - Identity manages the cookie
    [Display(Name = "Remember this device")]
    public bool RememberDevice { get; set; }
    //Not shown on form - passed back through hidden field after login redirect
    public string? MaskedEmail { get; set; }
}
