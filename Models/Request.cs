using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class Request
{
    [Key]
    [Display(Name = "Request ID")]
    public int RequestID { get; set; }

    [Display(Name = "User ID")]
    public string UserID { get; set; } = string.Empty;
    public User? User { get; set; }

    [Display(Name = "Item ID")]
    public int? ItemID { get; set; }// TQ- requested items won't have an ID just yet
    public Item? Item { get; set; }
    //TQ- User provides an Item name, has to be given
    [Required(ErrorMessage = "Please provide the Item Name")]
    [Display(Name = "Item Name")]
    [StringLength(50, ErrorMessage = "Item name cannot exceed 50 characters")]
    public string ItemName { get; set; } = string.Empty;
    //TQ- User picks a category, has to be selected
    [Required(ErrorMessage = "Please select a Category")]
    [Display(Name = "Category")]
    public int CategoryID { get; set; }
    public Category? Category { get; set; }
    //TQ- User can provide a location, but Staff maybe can set one afterwards
    [Display(Name = "Recycling Locations")]
    [StringLength(1000, ErrorMessage = "Locations cannot exceed 1000 characters")]
    public string RecyclingLocations { get; set; } = string.Empty;
    //TQ- Image, for now its just a BLOB? But: https://github.com/tlearm15/UnMuddled/issues/41
    [Display(Name = "Image")]
    public string? ImageUrl { get; set; }
    //TQ- User doesn't set this, its always Pending until Staff says otherwise.
    [Display(Name = "Status")]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    //TQ- Date submitted
    [Display(Name = "Submitted On")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
    public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;
}