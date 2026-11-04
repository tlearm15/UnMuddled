using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Please enter your password to confirm account deletion")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
