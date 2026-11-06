using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

// WM - making a new class for the user's history which contains the items this user has disposed. one to one with the user, many to many with items.
public class UserHistory
{
    [Key]
    [Display(Name = "User History ID")]
    public int UserHistoryId { get; set; }

    [Display(Name = "User ID")]
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    // Navigation properties#
    public ICollection<UserHistoryItem> UserHistoryItems { get; set; } = new List<UserHistoryItem>();
}