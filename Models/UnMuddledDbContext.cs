using GroupProject.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GroupProject.Models;

public class UnMuddledDbContext(
    DbContextOptions<UnMuddledDbContext> options,
    IEncryptionKeyProvider keyProvider,
    IEncryptionService encryption
    ) : IdentityDbContext<User>(options)
{
    /// <summary>
    /// TQ- This is the encryption key obtained from where the key lives
    /// to be used when encrypting/decrypting User information
    /// for when it is saved to/fetched from the DB
    /// </summary>
    private readonly byte[] _key = keyProvider.GetKey();
    /// <summary>
    /// TQ- Automagically makes it so the address can be encrypted/decrypted when
    /// saved to/fetched from Db - it should require no changes to Models and stuff
    /// but a new migration is likely.
    /// TQ(#83)- EncryptOnce makes sure no double-encryption happens when a
    /// already-encrypted row is saved back
    /// DecryptOrPassthrough keeps the old plaintext rows (registered before this feature) readable
    /// </summary>
    /// <returns></returns>
    private ValueConverter<string, string> EncryptedField() =>
        new( v =>  string.IsNullOrEmpty( v ) ? v : encryption.EncryptOnce( v, _key ),
            v =>  string.IsNullOrEmpty( v ) ? v : encryption.DecryptOrPassthrough( v, _key ) );

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<DisposalLocation> DisposalLocations => Set<DisposalLocation>();
    public DbSet<ItemDisposal> ItemDisposals => Set<ItemDisposal>();
    public DbSet<UserHistory> UserHistories => Set<UserHistory>();
    public DbSet<UserHistoryItem> UserHistoryItems => Set<UserHistoryItem>();
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
        builder.Entity<User>()
            .HasOne(u => u.UserHistory)
            .WithOne(uh => uh.User)
            .HasForeignKey<UserHistory>(uh => uh.UserId);

        // WM - adding in the junction table for many to many between userhistory and items
        builder.Entity<UserHistoryItem>()
            .HasOne(uhi => uhi.UserHistory)
            .WithMany(uh => uh.UserHistoryItems)
            .HasForeignKey(uhi => uhi.UserHistoryId);

        builder.Entity<UserHistoryItem>()
            .HasOne(uhi => uhi.Item)
            .WithMany(i => i.UserHistoryItems)
            .HasForeignKey(uhi => uhi.ItemId);

        // TQ- User location fields for encryption/decryption
        builder.Entity<User>(user =>
        {   // HasMaxLength <- excessive size for address lines, but there needs to be room for the ciphertext when it goes up on the DB.
            user.Property( u => u.AddressLine1 ).HasMaxLength( 300 ).HasConversion( EncryptedField() );
            user.Property( u => u.AddressLine2 ).HasMaxLength( 300 ).HasConversion( EncryptedField() );
            user.Property( u => u.City ).HasMaxLength( 300 ).HasConversion( EncryptedField() );
            user.Property( u => u.PostCode ).HasMaxLength( 300 ).HasConversion( EncryptedField() );
        });
    }
}