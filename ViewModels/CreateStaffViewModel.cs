using GroupProject.Models;
using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels
{
    public class CreateStaffViewModel
    {
        [Required(ErrorMessage = "Please enter a valid Email Address")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage =("Please enter a valid password for the account"))]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please provide your First Name")]
        [Display(Name = "First Name")]
        [StringLength(55, ErrorMessage = "First name cannot exceed 55 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide your Last Name")]
        [Display(Name = "Last Name")]
        [StringLength(95, ErrorMessage = "Last name cannot exceed 95 characters")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Address Line 1")]
        [StringLength(75)]
        [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9\s,.'/-]*$",
                ErrorMessage = "Please provide a valid UK Address")]
        public string AddressLine1 { get; set; } = string.Empty;

        [Display(Name = "Address Line 2")]
        [StringLength(75)]
        public string? AddressLine2 { get; set; }//Marked as Optional to match real world Addresses -RW

        [Display(Name = "City")]
        [StringLength(75)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide your Post Code")]
        //expression to check if the postcode is valid - TL
        [RegularExpression(
            @"^(GIR\s?0AA|(?:[A-Z]{1,2}[0-9][A-Z0-9]?\s?[0-9][ABD-HJLNP-UW-Z]{2}))$",
            ErrorMessage = "Please enter a valid UK postcode"
        )]
        [Display(Name = "Post Code")]
        [StringLength(25)]
        public string PostCode { get; set; } = string.Empty;

        [Display(Name = "User Role")]
        public UserRole UserRole { get; set; } = UserRole.Member;

        [Display(Name = "Date joined")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        public DateTime JoinedOn { get; set; } = DateTime.UtcNow;

        //Ensures that the user provides full address info during Registration and Editing-RW
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(AddressLine1) || string.IsNullOrWhiteSpace(City))
            {
                yield return new ValidationResult("Please provide your full address", new[] { nameof(AddressLine1), nameof(City) });
            }
        }
    }
}
