using System.Diagnostics;
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

    public string Encrypt(string plainText, byte[] key)
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

    public string Decrypt(string cipherText, byte[] key)
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
    /// <summary>
    /// TQ- unit test, but I don't know how to wire this up
    /// </summary>
    public void Test()
    {
        const string test = "This is a test";
        var key = RandomNumberGenerator.GetBytes( 1 << 8 ); // 256-bit key
        var secret = Encrypt( test, key );
        Console.WriteLine( "Encrypted text: ${0}", secret );
        Debug.Assert( test == Decrypt( secret, key ) );
    }
}

