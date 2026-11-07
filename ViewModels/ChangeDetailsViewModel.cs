using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels;

public class ChangeDetailsViewModel//ViewModel for changing user details -RW
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }//Marked as Optional to match real world Addresses -RW
    public string? City { get; set; }
    public string? PostCode { get; set; }
    // TQ- 2FA checkbox
    [Display(Name = "Two-Factor Authentication (email codes at login)")]
    public bool Is2FAEnabled { get; init; }
}
