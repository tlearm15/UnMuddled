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
    public int ItemID { get; set; }
    public Item? Item { get; set; }
}