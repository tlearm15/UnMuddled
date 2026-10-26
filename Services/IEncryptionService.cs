namespace GroupProject.Services;

public interface IEncryptionService
{
    /// <summary>
    /// TQ- Encrypts text with a given key
    /// </summary>
    /// <param name="plainText">The text you want to encrypt</param>
    /// <param name="key">the key used to encrypt it (the same key needs to be used to decrypt)</param>
    /// <returns>The text in encrypted form (secret)</returns>
    public string Encrypt(string plainText, byte[] key);
    /// <summary>
    /// TQ- Decrypts the given encrypted text using the same key that was used to encrypt it
    /// </summary>
    /// <param name="cipherText">The encrypted text</param>
    /// <param name="key">The key used to encrypt it</param>
    /// <returns>The original text before it was encrypted</returns>
    public string Decrypt(string cipherText, byte[] key);
}
