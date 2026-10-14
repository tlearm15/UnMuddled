using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    // Navigation property for relationships
    public ICollection<ProducDisposal> ProducDisposals { get; set; } = new List<ProducDisposal>();
}