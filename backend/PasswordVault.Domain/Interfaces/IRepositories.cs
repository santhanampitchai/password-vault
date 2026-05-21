using PasswordVault.Domain.Entities;

namespace PasswordVault.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> ExistsAsync(string email, CancellationToken ct = default);
}

public interface IAccountRepository
{
    Task<IEnumerable<Account>> GetAllByUserAsync(int userId, CancellationToken ct = default);
    Task<Account?> GetByIdAsync(int accountId, int userId, CancellationToken ct = default);
    Task<Account> AddAsync(Account account, CancellationToken ct = default);
    Task UpdateAsync(Account account, CancellationToken ct = default);
    Task DeleteAsync(int accountId, int userId, CancellationToken ct = default);
    Task<int> CountByUserAsync(int userId, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IAccountRepository Accounts { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
