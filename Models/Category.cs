using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class Category
{
    [Key]
    [Display(Name = "Category ID")]
    public int CategoryID { get; set; }

    [Required(ErrorMessage = "Please provide the Category Name")]
    [Display(Name = "Category Name")]
    [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
    public string CategoryName { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Item> Items { get; set; } = new List<Item>();
}