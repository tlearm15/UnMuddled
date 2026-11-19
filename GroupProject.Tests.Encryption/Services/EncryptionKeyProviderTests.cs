using System.Reflection;
using GroupProject.Services;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Services;
/// <summary>
/// TQ-
/// Tests how the app reads and checks the encryption key. The provider remembers the key
/// between calls, so each test clears that saved value first to keep the tests independent
/// of the order they run in.
/// </summary>
[TestFixture]
public class EncryptionKeyProviderTests
{
    private const string EnvName = "ENCRYPTION_KEY";
    private string? _originalValue;
    private FieldInfo _cacheField = null!;

    [SetUp]
    public void SetUp()
    {
        _originalValue = Environment.GetEnvironmentVariable(EnvName);
        _cacheField = typeof(EncryptionKeyProvider).GetField("_key",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("static _key cache field not found");
        _cacheField.SetValue(null, null); // clear the saved key so each test starts fresh
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(EnvName, _originalValue);
        _cacheField.SetValue(null, null);
    }

    [Test]
    public void GetKey_NoEnvironmentVariable_Throws()
    {
        Environment.SetEnvironmentVariable(EnvName, null);
        Assert.Throws<InvalidOperationException>(() => new EncryptionKeyProvider().GetKey());
    }

    [Test]
    public void GetKey_MissingEnvironmentVariable_ErrorMentionsEnvName()
    {
        Environment.SetEnvironmentVariable(EnvName, null);

        var ex = Assert.Throws<InvalidOperationException>(() => new EncryptionKeyProvider().GetKey());
        Assert.That(ex!.Message, Does.Contain("ENCRYPTION_KEY not set"));
    }

    [Test]
    public void GetKey_ValidBase64_32Bytes_ReturnsKey()
    {
        var key = new byte[32];
        new Random(42).NextBytes(key);
        Environment.SetEnvironmentVariable(EnvName, Convert.ToBase64String(key));
        var result = new EncryptionKeyProvider().GetKey();
        Assert.That(result, Is.EqualTo(key));
        Assert.That(result.Length, Is.EqualTo(32), "AES-256 requires exactly 32 bytes");
    }

    [Test]
    public void GetKey_CachesAcrossCalls_SameInstance()
    {
        var key = new byte[32];
        new Random(1).NextBytes( key );
        Environment.SetEnvironmentVariable( EnvName, Convert.ToBase64String( key ) );
        var provider = new EncryptionKeyProvider();
        var first = provider.GetKey();
        var second = provider.GetKey();
        Assert.That( second, Is.SameAs( first ), "key must be loaded once and cached" );
    }

    [Test]
    public void GetKey_WrongSize_AfterBase64Decode_Throws()
    {
        var shortKey = new byte[16]; // too short for the key this app requires
        Environment.SetEnvironmentVariable(EnvName, Convert.ToBase64String(shortKey));
        var ex = Assert.Throws<InvalidOperationException>( () => new EncryptionKeyProvider().GetKey() );
        Assert.That( ex!.Message, Does.Contain( "32 bytes"));
    }

    [Test]
    public void GetKey_NotBase64_ThrowsFormatException()
    {
        Environment.SetEnvironmentVariable( EnvName, "this is not base64!!");
        Assert.Throws<FormatException>( () => new EncryptionKeyProvider().GetKey());
    }

    [Test]
    public void GetKey_ZeroLengthKey_Throws()
    {
        Environment.SetEnvironmentVariable( EnvName, Convert.ToBase64String([]) );
        var ex = Assert.Throws<InvalidOperationException>( () => new EncryptionKeyProvider().GetKey() );
        Assert.That( ex!.Message, Does.Contain( "got 0" ) );
    }
}
