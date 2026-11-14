using GroupProject.Services;

namespace GroupProject.Tests.Encryption.Infrastructure;

/// <summary>
/// Fixed, deterministic 256-bit test key.
/// Never reads environment variables, so unit/integration tests never depend on ENCRYPTION_KEY.
/// </summary>
public sealed class FakeEncryptionKeyProvider : IEncryptionKeyProvider
{
    // Non-zero, fixed key so ciphertext is stable across test runs.
    private static readonly byte[] TestKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
        0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
    ];

    public byte[] GetKey() => _key ?? TestKey;

    private byte[]? _key;

    /// <summary>Overrides the key for special-case tests (e.g. key rotation scenarios).</summary>
    public void OverrideKey(byte[] key) => _key = key;
}
