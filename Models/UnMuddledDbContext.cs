using GroupProject.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GroupProject.Models;

public class UnMuddledDbContext(
    DbContextOptions<UnMuddledDbContext> options,
    IEncryptionKeyProvider keyProvider)
    : IdentityDbContext<User>(options)
{
    /// <summary>
    /// TQ- This is the encryption key obtained from where the key lives
    /// to be used when encrypting/decrypting User information
    /// for when it is saved to/fetched from the DB
    /// todo: wiring up to the actual DB operations further down
    /// </summary>
    private readonly byte[] _key = keyProvider.GetKey();
    /// <summary>
    /// TQ- Automagically makes it so the address can be encrypted/decrypted when
    /// saved to/fetched from Db - it should require no changes to Models and stuff
    /// but a new migration is likely
    /// </summary>
    /// <returns></returns>
    private ValueConverter<string, string> EncryptedField() =>
        new( v => new EncryptionService().Encrypt( v, _key ),
            v => new EncryptionService().Decrypt( v, _key ) );

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<DisposalLocation> DisposalLocations => Set<DisposalLocation>();
    public DbSet<ItemDisposal> ItemDisposals => Set<ItemDisposal>();
    public DbSet<ProductHistory> ProductHistories => Set<ProductHistory>();
    public DbSet<Request> Requests => Set<Request>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
        // TQ- todo: wire the address stuff to the builder
        // something like user.Property( u -> u.AddressLine1 ).HasConversion( EncryptedField() )
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