using Microsoft.EntityFrameworkCore;
using PersonalExpense.Domain.Entities;

namespace PersonalExpense.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User configurations
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        // Account configurations
        modelBuilder.Entity<Account>()
            .HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Category configurations
        modelBuilder.Entity<Category>()
            .HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Transaction configurations
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Account)
            .WithMany(a => a.FromTransactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.ToAccount)
            .WithMany(a => a.ToTransactions)
            .HasForeignKey(t => t.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Budget configurations
        modelBuilder.Entity<Budget>()
            .HasOne(b => b.User)
            .WithMany(u => u.Budgets)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Category)
            .WithMany(c => c.Budgets)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Unique constraint for user+month+year+category in budgets
        modelBuilder.Entity<Budget>()
            .HasIndex(b => new { b.UserId, b.Month, b.Year, b.CategoryId })
            .IsUnique();
    }
}