namespace GroupProject.Services;

public interface IEncryptionService
{
    /// <summary>TQ- Once only encryption: if the value already decrypts cleanly with this key it
    /// must be ciphertext, so it is stored unchanged; otherwise it is plaintext and gets
    /// encrypted exactly once (Issue #83).</summary>
    public string EncryptOnce(string value, byte[] key);
    /// <summary>TQ- Decrypt-or-passthrough: values that are not valid ciphertext for this key
    /// (e.g. legacy plaintext rows from before the AddressEncryption feature, Issue #83) are
    /// returned unchanged instead of throwing, so users with legacy rows can still log in.</summary>
    /// <param name="value">The value read from the DB (ciphertext or legacy plaintext)</param>
    /// <param name="key">The key used to encrypt it</param>
    /// <returns>The decrypted text, or the original value if it could not be decrypted</returns>
    public string DecryptOrPassthrough(string value, byte[] key);
    /// TQ- Quick self-check of the encryption round-trip + tamper detection.
    /// Returns true if everything behaves as expected.
    /// </summary>
    public bool Test();
}
