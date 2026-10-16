using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Models;
public class UnMuddledDbContext : IdentityDbContext<User>
{
    public UnMuddledDbContext(DbContextOptions<UnMuddledDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DisposalLocation> DisposalLocations => Set<DisposalLocation>();
    public DbSet<ProducDisposal> ProducDisposals => Set<ProducDisposal>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
  
}