using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please provide your First Name")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide your Last Name")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please provide your Email")]
        [EmailAddress]
        public string Email { get; set; }
        [Required(ErrorMessage = "Please provide your Password")]
        [DataType(DataType.Password)]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Password must have atleast 8 Characters")]
        [Compare("ConfirmPassword", ErrorMessage = "Passwords do not match")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Please confirm your Password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; } = string.Empty;
        [Display(Name = "Address Line 2")]
        public string AddressLine2 { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please provide your City")]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please provide your Post Code")]
        [RegularExpression(
            @"^(GIR\s?0AA|(?:[A-Z]{1,2}[0-9][A-Z0-9]?\s?[0-9][ABD-HJLNP-UW-Z]{2}))$",
            ErrorMessage = "Please enter a valid UK postcode"
        )]
        [Display(Name = "Post Code")]
        public string PostCode { get; set; } = string.Empty;
        //Ensures that the user provides full address info during Registration and Editing-RW
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(AddressLine1) || string.IsNullOrWhiteSpace(AddressLine2) || string.IsNullOrWhiteSpace(City))
            {
                yield return new ValidationResult("Please provide your full address", new[] { nameof(AddressLine1), nameof(AddressLine2), nameof(City) });
            }
        }

        [Display(Name = "Date joined")]
        [DataType((DataType.DateTime))]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        public DateTime JoinedOn { get; set; } = DateTime.UtcNow;
    }
}
