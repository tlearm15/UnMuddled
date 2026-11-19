using GroupProject.Services;

namespace GroupProject.Tests.Encryption.Infrastructure;

/// <summary>
/// A fixed test encryption key. It never reads the environment, so tests do not depend on
/// the ENCRYPTION_KEY setting being present.
/// </summary>
public sealed class FakeEncryptionKeyProvider : IEncryptionKeyProvider
{
    // A fixed, non-empty key keeps the encrypted test values predictable.
    private static readonly byte[] TestKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
        0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
    ];

    public byte[] GetKey() => TestKey;
}
