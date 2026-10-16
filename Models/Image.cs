using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class Image
{
    [Key]
    [Display(Name = "Image ID")]
    public int ImageID { get; set; }

    [Required(ErrorMessage = "Image data is required")]
    [Display(Name = "Image Data")]
    //Image will be stored as a blob (binary large object), might be changed later depending on DB performance - TL
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    [Display(Name = "Is Primary")]
    public bool IsPrimary { get; set; }

    [Display(Name = "Item ID")]
    public int ItemID { get; set; }
    public Item? Item { get; set; }
}