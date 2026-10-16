using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class DisposalLocation
{
    [Key]
    [Display(Name = "Disposal ID")]
    public int DisposalID { get; set; }

    [Required(ErrorMessage = "Please provide the Location Name")]
    [Display(Name = "Location Name")]
    [StringLength(90, ErrorMessage = "Location name cannot exceed 90 characters")]
    public string LocationName { get; set; } = string.Empty;

    [Display(Name = "Address Line 1")]
    [StringLength(90)]
    public string AddressLine1 { get; set; } = string.Empty;

    [Display(Name = "Address Line 2")]
    [StringLength(75)]
    public string AddressLine2 { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide the Town")]
    [Display(Name = "Town")]
    [StringLength(50)]
    public string Town { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide the Postcode")]
    [RegularExpression(
        @"^(GIR\s?0AA|(?:[A-Z]{1,2}[0-9][A-Z0-9]?\s?[0-9][ABD-HJLNP-UW-Z]{2}))$",
        ErrorMessage = "Please enter a valid UK postcode"
    )]
    [Display(Name = "Postcode")]
    [StringLength(35)]
    public string Postcode { get; set; } = string.Empty;

    [Display(Name = "Latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public decimal Latitude { get; set; }

    [Display(Name = "Longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public decimal Longitude { get; set; }

    // Navigation property
    public ICollection<ItemDisposal> ItemDisposals { get; set; } = new List<ItemDisposal>();
}