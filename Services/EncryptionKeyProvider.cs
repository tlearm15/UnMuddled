namespace GroupProject.Services;

/// <summary>TQ- Loads the pre-made encryption key from an external secret location.
/// Reads it once, caches it and hands out the same bytes forever after.</summary>
public class EncryptionKeyProvider : IEncryptionKeyProvider
{
    private static byte[]? _key;

    public byte[] GetKey() => _key ??= Load();

    private static byte[] Load() =>
        Convert.FromBase64String(
            Environment.GetEnvironmentVariable( "ENCRYPTION_KEY" )
            ?? throw new InvalidOperationException( "ENCRYPTION_KEY not set" ) );
}