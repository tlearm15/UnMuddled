using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class ProducDisposal
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int DisposalLocationId { get; set; }
    public DisposalLocation? DisposalLocation { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime DisposedAt { get; set; } = DateTime.UtcNow;
}