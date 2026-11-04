using System.ComponentModel.DataAnnotations;
namespace GroupProject.ViewModels;

// jm: simple forget password view model
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Please provide an Email Address")]
    [EmailAddress]
    public string Email { get; set; } =  string.Empty;
}

