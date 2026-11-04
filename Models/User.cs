using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupProject.Models;
//inherits IValidatableObject so that multiple properties can be validated - this can be further expanded on later - TL
public class User : IdentityUser
{
    [Display(Name = "First Name")]
    [StringLength(55, ErrorMessage = "First name cannot exceed 55 characters")]
    public string? FirstName { get; set; } = string.Empty;

    [Display(Name = "Last Name")]
    [StringLength(95, ErrorMessage = "Last name cannot exceed 95 characters")]
    public string? LastName { get; set; } = string.Empty;

    [Display(Name = "Address Line 1")]
    [StringLength(75)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9\s,.'/-]*$",
            ErrorMessage = "Please provide a valid UK Address")]
    public string? AddressLine1 { get; set; } = string.Empty;

    [Display(Name = "Address Line 2")]
    [StringLength(75)]
    public string? AddressLine2 { get; set; }//Marked as Optional to match real world Addresses -RW

    [Display(Name = "City")]
    [StringLength(75)]
    public string? City { get; set; } = string.Empty;

    //expression to check if the postcode is valid - TL
    [RegularExpression(
        @"^(GIR\s?0AA|(?:[A-Z]{1,2}[0-9][A-Z0-9]?\s?[0-9][ABD-HJLNP-UW-Z]{2}))$",
        ErrorMessage = "Please enter a valid UK postcode"
    )]
    [Display(Name = "Post Code")]
    [StringLength(25)]
    public string? PostCode { get; set; } = string.Empty;

    [Display(Name = "User Role")]
    public UserRole UserRole { get; set; } = UserRole.Member;

    [Display(Name = "2FA Enabled")]
    public bool Is2FAEnabled { get; set; }

    [Display(Name = "Date joined")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
    public DateTime JoinedOn { get; set; } = DateTime.UtcNow;

    [Display(Name = "History ID")]
    public int? HistoryID { get; set; }

    

    //
    /// <summary>
    /// Updated from Roberts old code - Moved Joined at above and ProductDisposals is now ProductHistory with one to one relationship
    /// This is so one user only has one product history
    /// - TL
    /// </summary>
    
    // Navigation properties
    public ProductHistory? ProductHistory { get; set; }
    public ICollection<Request> Requests { get; set; } = new List<Request>();
}