using GroupProject.Models;
using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels
{
    public class RequestViewModel// Request view Model for request creation - DMQ
    {
        public int RequestID { get; set; }

        [Required(ErrorMessage = "Please provide the Item Name")]
        [Display(Name = "Item Name")]
        [StringLength(50, ErrorMessage = "Item name cannot exceed 50 characters")]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a Category")]
        [Display(Name = "Category")]
        public int CategoryID { get; set; }

        [Display(Name = "Recycling Locations")]
        [StringLength(1000, ErrorMessage = "Locations cannot exceed 1000 characters")]
        public string RecyclingLocations { get; set; } = string.Empty;

        [Display(Name = "Room Tag")]
        public RoomTag RoomTag { get; set; } = RoomTag.Other;

        [Display(Name = "Upload Image")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImageUrl { get; set; }
    }
}
