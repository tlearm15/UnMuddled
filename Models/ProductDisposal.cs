using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class ProductDisposal
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int DisposalLocationId { get; set; }
    public DisposalLocation? DisposalLocation { get; set; }

    [Display(Name = "User ID")]
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    public DateTime DisposedAt { get; set; } = DateTime.UtcNow;
}