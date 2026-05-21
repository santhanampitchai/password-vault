using FluentAssertions;
using Microsoft.Extensions.Options;
using PasswordVault.Infrastructure.Security;
using Xunit;

namespace PasswordVault.Tests.Security;

public class AesEncryptionServiceTests
{
    private readonly AesEncryptionService _sut;

    public AesEncryptionServiceTests()
    {
        var opts = Options.Create(new SecuritySettings
        {
            EncryptionKey = "TestKey32CharsLongForUnitTesting!",  // exactly 32 chars
            JwtSecret     = "AnyValueForTestingPurposesOnly12345678"
        });
        _sut = new AesEncryptionService(opts);
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentCipherTextEachCall()
    {
        // Arrange
        const string plainText = "MySecretPassword123!";

        // Act
        var (cipher1, iv1) = _sut.Encrypt(plainText);
        var (cipher2, iv2) = _sut.Encrypt(plainText);

        // Assert – different IV each time means different cipher
        iv1.Should().NotBe(iv2);
        cipher1.Should().NotBe(cipher2);
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalPlainText()
    {
        // Arrange
        const string plainText = "SuperSecretP@ssw0rd!";

        // Act
        var (cipher, iv) = _sut.Encrypt(plainText);
        var decrypted    = _sut.Decrypt(cipher, iv);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_WithWrongIV_ShouldThrow()
    {
        // Arrange
        var (cipher, _) = _sut.Encrypt("hello");
        var wrongIV      = Convert.ToBase64String(new byte[16]); // all-zero IV

        // Act
        var act = () => _sut.Decrypt(cipher, wrongIV);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("This is a longer sentence that spans many characters to test block boundaries!")]
    public void RoundTrip_ShouldWorkForVariousLengths(string input)
    {
        var (cipher, iv) = _sut.Encrypt(input);
        var result       = _sut.Decrypt(cipher, iv);
        result.Should().Be(input);
    }
}
