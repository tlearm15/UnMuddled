namespace GroupProject.Models;

using Microsoft.EntityFrameworkCore;

public class UnMuddledDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DisposalLocation> DisposalLocations => Set<DisposalLocation>();
    public DbSet<ProducDisposal> ProducDisposals => Set<ProducDisposal>();

    public UnMuddledDbContext(DbContextOptions<UnMuddledDbContext> options) : base(options)
    {
    }
}