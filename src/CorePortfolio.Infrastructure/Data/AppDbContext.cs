using CorePortfolio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<MarketAsset> MarketAssets => Set<MarketAsset>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<PortfolioSnapshot> PortfolioSnapshots => Set<PortfolioSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuration for relationships and constraints
        modelBuilder.Entity<User>()
            .HasMany(u => u.Portfolios)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Portfolio>()
            .HasMany(p => p.Assets)
            .WithOne(a => a.Portfolio)
            .HasForeignKey(a => a.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Portfolio>()
            .HasMany(p => p.Transactions)
            .WithOne(t => t.Portfolio)
            .HasForeignKey(t => t.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Portfolio>()
            .HasMany<PortfolioSnapshot>()
            .WithOne(s => s.Portfolio)
            .HasForeignKey(s => s.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Asset)
            .WithMany()
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MarketAsset>()
            .HasOne(m => m.Category)
            .WithMany()
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Asset>()
            .HasOne(a => a.MarketAsset)
            .WithMany()
            .HasForeignKey(a => a.MarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SystemSetting>()
            .HasKey(s => s.Key);

        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { Key = "USD_TO_VND", Value = "26309", Description = "Exchange rate from USD to VND", LastUpdated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
