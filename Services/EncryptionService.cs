using System.Security.Cryptography;
using System.Text;

namespace GroupProject.Services;
/// <summary>
/// TQ- Encryptions service for Encrypting and Decrypting strings using a key
/// Usage:
/// byte[] key = RandomNumberGenerator.GetBytes( 32 ); // 256-bit key
/// string secret = EncryptionService.Encrypt("Sensitive customer data", key);
/// Console.WriteLine(secret);
/// Console.WriteLine(EncryptionService.Decrypt(secret, key));
/// </summary>
public class EncryptionService : IEncryptionService
{
    /// <summary>
    /// TQ- Encrypts text with a given key
    /// </summary>
    /// <param name="plainText">The text you want to encrypt</param>
    /// <param name="key">the key used to encrypt it (the same key needs to be used to decrypt)</param>
    /// <returns>The text in encrypted form (secret)</returns>
    /// <summary>
    private static string Encrypt(string plainText, byte[] key)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        // A 12-byte nonce is the recommended size for AES-GCM.
        // CRITICAL: never reuse a nonce with the same key.
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var cipherBytes = new byte[plainBytes.Length];

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        // Pack: [nonce][tag][ciphertext] so decryption has everything it needs.
        var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }
    /// TQ- Decrypts the given encrypted text using the same key that was used to encrypt it
    /// </summary>
    /// <param name="cipherText">The encrypted text</param>
    /// <param name="key">The key used to encrypt it</param>
    /// <returns>The original text before it was encrypted</returns>
    /// <summary>
    private static string Decrypt(string cipherText, byte[] key)
    {
        var data = Convert.FromBase64String(cipherText);
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;
        var nonce = data.AsSpan(0, nonceSize);
        var tag = data.AsSpan(nonceSize, tagSize);
        var cipherBytes = data.AsSpan(nonceSize + tagSize);
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, tagSize);
        // Throws CryptographicException if the data was tampered with.
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public string EncryptOnce(string value, byte[] key)
    {
        if( string.IsNullOrEmpty( value ) )
            return value;
        
        try
        {   // TQ- SAFETY NET: Check for whether `value` is already cipher text to prevent multiple encryption
            // If `value` truly is cipher text, try decryption and it should succeed.
            // If so return just the cipher text as is.
            Decrypt( value, key );
            return value;
        }
        catch( Exception ex ) when (
            ex is FormatException          // not base64 at all -> plaintext
              or CryptographicException    // base64 but bad tag / bad length -> plaintext
              or ArgumentException         // wrong carve sizes etc -> plaintext
              or ArgumentOutOfRangeException )
        {
            return Encrypt( value, key );
        }
    }
    
    public string DecryptOrPassthrough(string value, byte[] key)
    {
        if( string.IsNullOrEmpty( value ) )
            return value;
        // TQ- SAFETY NET: To make sure the cipher text is fully decrypted, do the decryption recursively
        // Chosen 8 cycles
        var current = value;
        for( var i = 0; i < 8; i++ )
        {
            try
            {
                current = Decrypt( current, key );
            }
            catch ( Exception ex ) when (
                ex is FormatException
                    or CryptographicException
                    or ArgumentException
                    or ArgumentOutOfRangeException )
            {
                return current;// not ciphertext -> plaintext (or peel complete)
            }
        }
        
        return current;// cap reached; return best-effort result
    }
    
    public bool Test()
    {
        const string message = "This is a test";
        var key = RandomNumberGenerator.GetBytes( 32 ); // 256-bit key
        var secret = Encrypt(message, key);
        Console.WriteLine($"Encrypted text: {secret}");

        if( Decrypt(secret, key) != message ) 
            return false;
        
        var bytes = Convert.FromBase64String( secret );
        bytes[^1] ^= 0xFF;// flip last byte
        
        try { Decrypt(Convert.ToBase64String(bytes), key); return false; }
        catch (CryptographicException) { return true; }             // tamper must throw
    }
}

