using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels
{
    public class ChangeDetailsViewModel//ViewModel for changing user details -RW
    {
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Please provide the first line of your address")]
        public string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }//Marked as Optional to match real world Addresses -RW
        [Required(ErrorMessage = "Please provide your city")]
        public string City { get; set; }
        [Required(ErrorMessage = "Please provide your post code")]
        public string PostCode { get; set; }
    }
}
