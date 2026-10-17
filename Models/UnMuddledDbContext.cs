using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Models;

public class UnMuddledDbContext : IdentityDbContext<User>
{
    public UnMuddledDbContext(DbContextOptions<UnMuddledDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<DisposalLocation> DisposalLocations => Set<DisposalLocation>();
    public DbSet<ItemDisposal> ItemDisposals => Set<ItemDisposal>();
    public DbSet<ProductHistory> ProductHistories => Set<ProductHistory>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductDisposal> ProductDisposals => Set<ProductDisposal>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
     
        //fixed typo in table name - TL
        builder.Entity<ProductDisposal>().ToTable("ProductDisposals");
        
        builder.Entity<ItemDisposal>()
            .HasKey(id => new { id.DisposalID, id.ItemID });

        builder.Entity<ItemDisposal>()
            .HasOne(id => id.DisposalLocation)
            .WithMany(dl => dl.ItemDisposals)
            .HasForeignKey(id => id.DisposalID);

        builder.Entity<ItemDisposal>()
            .HasOne(id => id.Item)
            .WithMany(i => i.ItemDisposals)
            .HasForeignKey(id => id.ItemID);

        // One to one relatioinship - User -> ProductHistory - TL
        builder.Entity<User>()
            .HasOne(u => u.ProductHistory)
            .WithOne(ph => ph.User)
            .HasForeignKey<ProductHistory>(ph => ph.UserID);

        //One to many -  ProductHistory -> Items - TL
        builder.Entity<ProductHistory>()
            .HasMany(ph => ph.Items)
            .WithOne(i => i.ProductHistory)
            .HasForeignKey(i => i.HistoryID)
            .IsRequired(false);
    }
}