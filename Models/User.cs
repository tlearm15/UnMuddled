using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class User
{
    [Key]
    public int Id { get; set; }
        
    public string Username { get; set; } = string.Empty;
        
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<ProducDisposal> ProducDisposals { get; set; } = new List<ProducDisposal>();
}