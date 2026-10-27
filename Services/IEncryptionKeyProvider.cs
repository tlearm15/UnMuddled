namespace GroupProject.Services;

/// <summary>TQ- Supplies the static encryption key used for DB field encryption.
/// The key is created/managed externally and must never change.</summary>
public interface IEncryptionKeyProvider
{
    /// <summary>
    /// TQ-
    /// Gets the externally managed 256-bit key.
    /// This will probably be from an env variable
    /// or a file stored in a super secret place
    /// </summary>
    byte[] GetKey();
}
