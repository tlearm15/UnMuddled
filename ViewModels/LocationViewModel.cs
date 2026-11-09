using System.ComponentModel.DataAnnotations;
using GroupProject.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GroupProject.ViewModels;

// jm: for Where to Recycle location map/form viewmodel
public class LocationViewModel
{
    // --- jm: Inputs ---
    public int? SelectedItemId { get; set; }
    public string? LocationSearch { get; set; }
    public bool UseSavedAddress { get; set; } 
    public bool HasSavedAddress { get; set; }
    
    // --- jm: Geolocation ---
    // jm: coordinates for Geolocation
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // --- jm: Dropdowns & Results ---
    public IEnumerable<SelectListItem> Items { get; set; } = new List<SelectListItem>(); // jm: dropdown list of items user can select
    public List<DisposalLocation> Locations { get; set; } = []; // jm: results of nearby locations found by the search
    public string? ItemName { get; set; }
}