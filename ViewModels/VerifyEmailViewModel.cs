using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels
{
    public class VerifyEmailViewModel//ViewModel for verifying email -RW
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }


        
    }
}
