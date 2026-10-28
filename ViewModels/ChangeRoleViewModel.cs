using GroupProject.Models;
using System.ComponentModel.DataAnnotations;

namespace GroupProject.ViewModels
{
    public class ChangeRoleViewModel
    {
        //View model to update the role of a user
        [Required]
        public string Id { get; set; }
        [Required]
        public UserRole userRole {  get; set; }
    }
}
