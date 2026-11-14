using System.Text;
using GroupProject.Services;
using GroupProject.Tests.Encryption.Infrastructure;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Services;

/// <summary>
/// Unit tests for the AES-GCM encryption logic behind IEncryptionService
/// (branch 7-user-address-encryption). Uses the public API only:
/// EncryptOnce / DecryptOrPassthrough / Test.
/// </summary>
[TestFixture]
public class EncryptionServiceTests
{
    private EncryptionService _sut = null!;
    private byte[] _key = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new EncryptionService();
        _key = new FakeEncryptionKeyProvider().GetKey();
    }

    // ---------------- Round-trip ----------------

    [Test]
    public void EncryptDecryptRoundTrip_OrdinaryText_ReturnsOriginal()
    {
        const string plain = "12 Trongate, Glasgow";
        var cipher = _sut.EncryptOnce(plain, _key);

        Assert.That(cipher, Is.Not.EqualTo(plain), "ciphertext must differ from plaintext");
        Assert.That(_sut.DecryptOrPassthrough(cipher, _key), Is.EqualTo(plain));
    }

    [Test]
    public void EncryptDecryptRoundTrip_SpecialCharacters_Preserved(
        [Values("42 Oak St, Apt #3!", "O'Brien & Sons Ltd.", "Peterborough, PE1 1AA",
            "日本語の住所テスト", "Ünïcödé Śt.", "a\tb\r\nc")] string plain)
    {
        var cipher = _sut.EncryptOnce(plain, _key);
        Assert.That(_sut.DecryptOrPassthrough(cipher, _key), Is.EqualTo(plain));
    }

    [Test]
    public void EncryptDecryptRoundTrip_EmptyAndNull_PassthroughUnchanged()
    {
        Assert.That(_sut.EncryptOnce(string.Empty, _key), Is.Empty);
        Assert.That(_sut.DecryptOrPassthrough(string.Empty, _key), Is.Empty);
        Assert.That(_sut.EncryptOnce(null!, _key), Is.Null);
        Assert.That(_sut.DecryptOrPassthrough(null!, _key), Is.Null);
    }

    [Test]
    public void EncryptDecryptRoundTrip_LongAddress_Succeeds()
    {
        var plain = new string('x', 299); // 300-char max DB column, per HasMaxLength(300)
        var cipher = _sut.EncryptOnce(plain, _key);
        Assert.That(_sut.DecryptOrPassthrough(cipher, _key), Is.EqualTo(plain));
    }

    // ---------------- Tamper detection ----------------

    [Test]
    public void TamperedCiphertext_NeverDecryptsToOriginal()
    {
        const string plain = "1 High Street, Edinburgh";
        var cipher = _sut.EncryptOnce(plain, _key);
        var bytes = Convert.FromBase64String(cipher);
        bytes[^1] ^= 0xFF; // flip last ciphertext byte
        var tampered = Convert.ToBase64String(bytes);

        // DecryptOrPassthrough deliberately does not throw on tamper (Issue #83 legacy
        // compatibility), but it must NOT hand back the original plaintext either -
        // the value is corrupt and must not silently look valid.
        var result = _sut.DecryptOrPassthrough(tampered, _key);
        Assert.That(result, Is.Not.EqualTo(plain));
    }

    [Test]
    public void TamperedCiphertext_FlippingEachRegion_NeverReturnsOriginal()
    {
        const string plain = "Flat 2, 22 Hope Street";
        var cipher = Convert.FromBase64String(_sut.EncryptOnce(plain, _key));

        // Flip a byte in the nonce (start), tag (middle) and ciphertext (end).
        foreach (var index in new[] { 0, 24, cipher.Length - 1 })
        {
            var copy = (byte[])cipher.Clone();
            copy[index] ^= 0xFF;
            var result = _sut.DecryptOrPassthrough(Convert.ToBase64String(copy), _key);
            Assert.That(result, Is.Not.EqualTo(plain), $"tampering at byte {index} went undetected");
        }
    }

    [Test]
    public void SelfCheck_Test_ReturnsTrue()
    {
        // EncryptionService.Test() exercises round-trip + tamper-must-throw internally.
        Assert.That(_sut.Test(), Is.True);
    }

    // ---------------- Wrong key ----------------

    [Test]
    public void DecryptWithWrongKey_DoesNotThrowAndDoesNotReturnPlaintext()
    {
        const string plain = "5 Sample Road, Dundee";
        var cipher = _sut.EncryptOnce(plain, _key);

        var wrongKey = new byte[32];
        wrongKey[0] = 0xAA;
        var result = _sut.DecryptOrPassthrough(cipher, wrongKey);

        Assert.That(result, Is.Not.EqualTo(plain), "decryption with the wrong key must not yield the plaintext");
    }

    // ---------------- EncryptOnce: no double encryption (Issue #83) ----------------

    [Test]
    public void EncryptOnce_AlreadyCiphertext_ReturnsUnchanged()
    {
        const string plain = "77 Encryption Way";
        var cipher1 = _sut.EncryptOnce(plain, _key);
        var cipher2 = _sut.EncryptOnce(cipher1, _key);

        Assert.That(cipher2, Is.EqualTo(cipher1), "EncryptOnce must not double-encrypt");
        Assert.That(_sut.DecryptOrPassthrough(cipher2, _key), Is.EqualTo(plain));
    }

    [Test]
    public void EncryptOnce_ProducesUniqueNonce_EveryCall()
    {
        const string plain = "Same address, different ciphertext";
        var c1 = _sut.EncryptOnce(plain, _key);
        var c2 = _sut.EncryptOnce(plain, _key);

        Assert.That(c2, Is.Not.EqualTo(c1), "each encryption must use a fresh nonce");
        Assert.That(_sut.DecryptOrPassthrough(c1, _key), Is.EqualTo(plain));
        Assert.That(_sut.DecryptOrPassthrough(c2, _key), Is.EqualTo(plain));
    }

    // ---------------- Ciphertext shape ----------------

    [Test]
    public void Ciphertext_IsBase64_PackedNonceTagCipher()
    {
        var cipher = _sut.EncryptOnce("Shape check", _key);
        var bytes = Convert.FromBase64String(cipher); // throws if not valid base64

        // 12-byte nonce (max) + 16-byte tag (max) + ciphertext >= plaintext length
        Assert.That(bytes.Length, Is.GreaterThanOrEqualTo(12 + 16 + "Shape check".Length));
    }

    [Test]
    public void Ciphertext_DoesNotContainPlaintextFragment()
    {
        const string fragment = "SecretLane";
        var cipher = _sut.EncryptOnce($"9 {fragment}, Aberdeen", _key);

        Assert.That(cipher, Does.Not.Contain(fragment));
    }
}
