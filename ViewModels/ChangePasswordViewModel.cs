using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels
{
    public class ChangePasswordViewModel//ViewModel for changing password -RW
    {
        [Required(ErrorMessage = "Please enter your current password")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }
        [Required(ErrorMessage = "Please enter your new password")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Password must have at least 8 characters")]
        [DataType(DataType.Password)]
        [Compare("ConfirmNewPassword", ErrorMessage = "Passwords do not match")]
        public string NewPassword { get; set; }
        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; }
    }
}
