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

public class AccountServiceTests
{
    private readonly Mock<IUnitOfWork>      _uowMock     = new();
    private readonly Mock<IAccountRepository> _repoMock  = new();
    private readonly Mock<IEncryptionService> _encMock   = new();
    private readonly Mock<IAuditService>    _auditMock   = new();
    private readonly AccountService         _sut;

    public AccountServiceTests()
    {
        _uowMock.Setup(u => u.Accounts).Returns(_repoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        _encMock.Setup(e => e.Encrypt(It.IsAny<string>()))
                .Returns(("CIPHER", "IV16BYTES_BASE64"));

        _sut = new AccountService(
            _uowMock.Object,
            _encMock.Object,
            _auditMock.Object,
            NullLogger<AccountService>.Instance);
    }

    [Fact]
    public async Task CreateAccountAsync_ValidRequest_ShouldCallAddAndSave()
    {
        // Arrange
        var request = new CreateAccountRequest(
            "Gmail", "user@gmail.com", "ENCRYPTED_PWD", "BASE64_IV",
            null, "Email", "https://gmail.com");

        _repoMock.Setup(r => r.AddAsync(It.IsAny<Account>(), default))
                 .ReturnsAsync((Account a, CancellationToken _) => a);

        // Act
        var result = await _sut.CreateAccountAsync(1, request);

        // Assert
        result.AccountName.Should().Be("Gmail");
        result.UserName.Should().Be("user@gmail.com");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Account>(), default), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        _encMock.Verify(e => e.Encrypt(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetAccountByIdAsync_NotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99, 1, default))
                 .ReturnsAsync((Account?)null);

        // Act
        var act = async () => await _sut.GetAccountByIdAsync(99, 1);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAccountAsync_ValidId_ShouldCallDelete()
    {
        // Arrange
        var account = new Account { AccountId = 5, UserId = 1, AccountName = "Test",
            UserName = "t@t.com", EncryptedPassword = "x", EncryptionIV = "y" };
        _repoMock.Setup(r => r.GetByIdAsync(5, 1, default)).ReturnsAsync(account);

        // Act
        await _sut.DeleteAccountAsync(5, 1);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(5, 1, default), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetAccountsAsync_WithSearch_ShouldFilterResults()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new() { AccountId=1, UserId=1, AccountName="Gmail",    UserName="a@a.com", EncryptedPassword="x", EncryptionIV="y", CreatedDate=DateTime.UtcNow },
            new() { AccountId=2, UserId=1, AccountName="Facebook", UserName="b@b.com", EncryptedPassword="x", EncryptionIV="y", CreatedDate=DateTime.UtcNow },
            new() { AccountId=3, UserId=1, AccountName="GitHub",   UserName="c@c.com", EncryptedPassword="x", EncryptionIV="y", CreatedDate=DateTime.UtcNow },
        };
        _repoMock.Setup(r => r.GetAllByUserAsync(1, default)).ReturnsAsync(accounts);

        // Act
        var result = await _sut.GetAccountsAsync(1, new AccountQueryParams(Search: "git"));

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().AccountName.Should().Be("GitHub");
        result.TotalCount.Should().Be(1);
    }
}
