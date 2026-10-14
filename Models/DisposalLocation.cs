using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class DisposalLocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    // Navigation property for relationships
    public ICollection<ProducDisposal> ProducDisposals { get; set; } = new List<ProducDisposal>();
}