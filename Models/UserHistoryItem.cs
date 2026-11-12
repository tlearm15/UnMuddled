using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

// WM - creating a junction table for a many to many between userhistory and item
public class UserHistoryItem
{
    [Key]
    public int UserHistoryItemId { get; set; }

    [Display(Name = "Date Disposed")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
    public DateTime DateDisposed { get; set; } = DateTime.UtcNow;

    [Display(Name = "Item ID")]
    public int ItemId { get; set; }
    public Item? Item { get; set; }

    [Display(Name = "User History ID")]
    public int UserHistoryId { get; set; }
    public UserHistory? UserHistory { get; set; }
}