using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class ProductHistory
{
    [Key]
    [Display(Name = "History ID")]
    public int HistoryID { get; set; }

    [Display(Name = "User ID")]
    public string UserID { get; set; } = string.Empty;
    public User? User { get; set; }

    [Display(Name = "Date Logged")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
    public DateTime DateDisposed { get; set; } = DateTime.Now;

    //Navigation property
    public ICollection<Item> Items { get; set; } = new List<Item>();
}