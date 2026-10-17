using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class Item
{
    [Key]
    [Display(Name = "Item ID")]
    public int ItemID { get; set; }

    [Required(ErrorMessage = "Please provide the Item Name")]
    [Display(Name = "Item Name")]
    [StringLength(50, ErrorMessage = "Item name cannot exceed 50 characters")]
    public string ItemName { get; set; } = string.Empty;

    [Display(Name = "Is Recyclable")]
    public bool IsRecyclable { get; set; }

    [Display(Name = "Category ID")]
    public int CategoryID { get; set; }
    public Category? Category { get; set; }

    [Display(Name = "Description")]
    [StringLength(450, ErrorMessage = "Description cannot exceed 450 characters")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Date Added")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
    public DateTime DateAdded { get; set; } = DateTime.Now;

    [Display(Name = "Room Tag")]
    //Sets RoomTag from the enum, defaulting to Other if none selected - TL
    public RoomTag RoomTag { get; set; } = RoomTag.Other;
    
    [Display(Name = "History ID")]
    public int? HistoryID { get; set; }
    public ProductHistory? ProductHistory { get; set; }

    // Navigation properties
    public ICollection<Image> Images { get; set; } = new List<Image>();
    public ICollection<ItemDisposal> ItemDisposals { get; set; } = new List<ItemDisposal>();
    public ICollection<Request> Requests { get; set; } = new List<Request>();
}