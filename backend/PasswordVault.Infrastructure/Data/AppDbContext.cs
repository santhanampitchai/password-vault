using Microsoft.EntityFrameworkCore;
using PasswordVault.Domain.Entities;

namespace PasswordVault.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User>    Users    => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ─── User ──────────────────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.UserId);
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.PasswordSalt).IsRequired();
            e.Property(u => u.CreatedDate).HasDefaultValueSql("GETDATE()");
            e.Property(u => u.IsActive).HasDefaultValue(true);
        });

        // ─── Account ───────────────────────────────────────────────────────
        mb.Entity<Account>(e =>
        {
            e.ToTable("Accounts");
            e.HasKey(a => a.AccountId);
            e.Property(a => a.AccountName).HasMaxLength(200).IsRequired();
            e.Property(a => a.UserName).HasMaxLength(300).IsRequired();
            e.Property(a => a.EncryptedPassword).IsRequired();
            e.Property(a => a.EncryptionIV).HasMaxLength(200).IsRequired();
            e.Property(a => a.Category).HasMaxLength(100);
            e.Property(a => a.WebsiteUrl).HasMaxLength(500);
            e.Property(a => a.CreatedDate).HasDefaultValueSql("GETDATE()");

            e.HasOne(a => a.User)
             .WithMany(u => u.Accounts)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
