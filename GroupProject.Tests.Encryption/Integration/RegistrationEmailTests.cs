using GroupProject.Tests.Encryption.Infrastructure;
using NUnit.Framework;

namespace GroupProject.Tests.Encryption.Integration;
/// <summary>
/// TQ-
/// Checks that registration records a confirmation email instead of sending a real one.
/// This is not about encryption, but it lives here because registration over HTTP is
/// already set up by <see cref="RegistrationTestBase"/>. Move it if a separate registration
/// test project is added later.
/// </summary>
[TestFixture]
public class RegistrationEmailTests : RegistrationTestBase
{
    [Test]
    public async Task RegistrationEmail_IsCaptured_NotSent()
    {
        await RegisterViaHttpAsync("alan@test.com", "Alan", "Turing");

        Assert.Multiple(() =>
        {
            Assert.That( Factory.EmailSender.Sent, Has.Count.EqualTo( 1 ),
                "registration must send exactly one confirmation email" );
            Assert.That( Factory.EmailSender.Sent[0].Email, Is.EqualTo( "alan@test.com" ) );
            Assert.That( Factory.EmailSender.Sent[0].Subject, Is.EqualTo( "Confirmation" ) );
            Assert.That( Factory.EmailSender.Sent[0].Html,
                Does.Contain( "ConfirmEmail" ).And.Contain( "userId=" ).And.Contain( "token=" ),
                "confirmation link must point at the ConfirmEmail action with a user id and token" );
        } );
    }
}
