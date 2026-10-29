using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels
{
    public class ChangeDetailsViewModel//ViewModel for changing user details -RW
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }//Marked as Optional to match real world Addresses -RW
        public string? City { get; set; }
        public string? PostCode { get; set; }
    }
}
