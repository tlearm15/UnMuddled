using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class Image
{
    [Key]
    [Display(Name = "Image ID")]
    public int ImageID { get; set; }

    [Required(ErrorMessage = "Image URL is required")]
    [Display(Name = "Image URL")]
    //Image will be stored as a blob (binary large object), might be changed later depending on DB performance - TL
    public string ImageURL { get; set; } = string.Empty;
    
    [Display(Name = "Is Primary")]
    public bool IsPrimary { get; set; }

    [Display(Name = "Item ID")]
    public int ItemID { get; set; }
    public Item? Item { get; set; }
}