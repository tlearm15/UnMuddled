using System.ComponentModel.DataAnnotations;
using GroupProject.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GroupProject.ViewModels;

// jm: for Where to Recycle location map/form viewmodel
public class LocationViewModel
{
    // --- jm: Inputs ---
    [Required(ErrorMessage = "Please select an item to recycle")]
    public int? SelectedItemId { get; set; }
    
    [StringLength(100, ErrorMessage = "Location search must be under 100 characters")]
    public string? LocationSearch { get; set; }
    
    // JM: Commented out as not in current scope to find locations based on user addresses
    //public bool UseSavedAddress { get; set; } 
   // public bool HasSavedAddress { get; set; }
    
    // --- jm: Geolocation ---
    // jm: coordinates for Geolocation
    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }
    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }

    // --- jm: Dropdowns & Results ---
    public IEnumerable<SelectListItem> Items { get; set; } = new List<SelectListItem>(); // jm: dropdown list of items user can select
    public List<DisposalLocation> Locations { get; set; } = []; // jm: results of nearby locations found by the search
    public string? ItemName { get; set; }
}