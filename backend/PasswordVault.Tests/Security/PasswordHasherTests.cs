using FluentAssertions;
using PasswordVault.Infrastructure.Security;
using Xunit;

namespace PasswordVault.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ShouldReturnNonEmptyHashAndSalt()
    {
        var (hash, salt) = _sut.HashPassword("P@ssw0rd!");
        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_SameInput_ShouldProduceDifferentHashes()
    {
        var (hash1, salt1) = _sut.HashPassword("SamePassword");
        var (hash2, salt2) = _sut.HashPassword("SamePassword");
        hash1.Should().NotBe(hash2);
        salt1.Should().NotBe(salt2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        const string pwd = "CorrectPassword!99";
        var (hash, salt) = _sut.HashPassword(pwd);
        _sut.VerifyPassword(pwd, hash, salt).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ShouldReturnFalse()
    {
        var (hash, salt) = _sut.HashPassword("CorrectPassword");
        _sut.VerifyPassword("WrongPassword", hash, salt).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ShouldReturnFalse()
    {
        var (hash, salt) = _sut.HashPassword("SomePassword");
        _sut.VerifyPassword("", hash, salt).Should().BeFalse();
    }
}
