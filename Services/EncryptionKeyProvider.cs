namespace GroupProject.Services;

/// <summary>TQ- Loads the pre-made encryption key from an external secret location.
/// Reads it once, caches it and hands out the same bytes forever after.</summary>
public class EncryptionKeyProvider : IEncryptionKeyProvider
{
    private static byte[]? _key;

    public byte[] GetKey() => _key ??= Load();

    private static byte[] Load()
    {
        var key = Convert.FromBase64String(
            Environment.GetEnvironmentVariable( "ENCRYPTION_KEY" )
            ?? throw new InvalidOperationException( "ENCRYPTION_KEY not set" ) );
        // Validate the size of the key is always the same
        if( key.Length != 32 ) // AES-256 requires exactly 32 bytes
            throw new InvalidOperationException(
                $"ENCRYPTION_KEY must be 32 bytes (256-bit) after base64-decoding, got {key.Length}. Generate with: openssl rand -base64 32");

        return key;
    }
}