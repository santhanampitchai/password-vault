using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PasswordVault.Application.DTOs;
using PasswordVault.Application.Interfaces;
using PasswordVault.Application.Services;
using PasswordVault.Domain.Entities;
using PasswordVault.Domain.Interfaces;
using Xunit;

namespace PasswordVault.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork>    _uowMock     = new();
    private readonly Mock<IUserRepository> _userRepo   = new();
    private readonly Mock<IPasswordHasher> _hasher     = new();
    private readonly Mock<ITokenService>  _tokens      = new();
    private readonly AuthService          _sut;

    public AuthServiceTests()
    {
        _uowMock.Setup(u => u.Users).Returns(_userRepo.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns("ACCESS_TOKEN");
        _tokens.Setup(t => t.GenerateRefreshToken()).Returns("REFRESH_TOKEN");

        _sut = new AuthService(_uowMock.Object, _hasher.Object, _tokens.Object,
                               NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_ShouldSucceed()
    {
        // Arrange
        _userRepo.Setup(r => r.ExistsAsync("new@test.com", default)).ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), default))
                 .ReturnsAsync((User u, CancellationToken _) => u);
        _hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns(("HASH", "SALT"));

        // Act
        var result = await _sut.RegisterAsync(new RegisterRequest("Test User", "new@test.com", "P@ss!1234"));

        // Assert
        result.AccessToken.Should().Be("ACCESS_TOKEN");
        result.User.Email.Should().Be("new@test.com");
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ShouldThrowInvalidOperation()
    {
        _userRepo.Setup(r => r.ExistsAsync("existing@test.com", default)).ReturnsAsync(true);

        var act = async () => await _sut.RegisterAsync(
            new RegisterRequest("X", "existing@test.com", "pass"));

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already exists*");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var user = new User { UserId = 1, Email = "user@test.com", FullName = "Test",
            PasswordHash = "HASH", PasswordSalt = "SALT", IsActive = true };
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com", default)).ReturnsAsync(user);
        _hasher.Setup(h => h.VerifyPassword("correct", "HASH", "SALT")).Returns(true);

        // Act
        var result = await _sut.LoginAsync(new LoginRequest("user@test.com", "correct"));

        // Assert
        result.AccessToken.Should().Be("ACCESS_TOKEN");
        result.RefreshToken.Should().Be("REFRESH_TOKEN");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowUnauthorized()
    {
        var user = new User { UserId = 1, Email = "u@t.com", FullName = "T",
            PasswordHash = "H", PasswordSalt = "S", IsActive = true };
        _userRepo.Setup(r => r.GetByEmailAsync("u@t.com", default)).ReturnsAsync(user);
        _hasher.Setup(h => h.VerifyPassword("wrong", "H", "S")).Returns(false);

        var act = async () => await _sut.LoginAsync(new LoginRequest("u@t.com", "wrong"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
