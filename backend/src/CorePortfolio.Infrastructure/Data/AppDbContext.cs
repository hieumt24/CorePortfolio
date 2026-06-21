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
    public DbSet<CashflowCategory> CashflowCategories => Set<CashflowCategory>();
    public DbSet<CashflowRecord> CashflowRecords => Set<CashflowRecord>();

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
            .HasMany(p => p.Snapshots)
            .WithOne(s => s.Portfolio)
            .HasForeignKey(s => s.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Portfolio>()
            .HasMany(p => p.CashflowRecords)
            .WithOne(c => c.Portfolio)
            .HasForeignKey(c => c.PortfolioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.CashflowRecords)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.CustomCategories)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CashflowRecord>()
            .HasOne(c => c.Category)
            .WithMany()
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CashflowRecord>()
            .HasOne(c => c.Transaction)
            .WithMany()
            .HasForeignKey(c => c.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);
            
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

        var fiatCategoryId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<AssetCategory>().HasData(
            new AssetCategory { Id = fiatCategoryId, Name = "Fiat", DefaultCurrency = "VND" }
        );

        modelBuilder.Entity<MarketAsset>().HasData(
            new MarketAsset 
            { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), 
                CategoryId = fiatCategoryId, 
                Symbol = "VND", 
                Name = "VND Cash", 
                CurrentPrice = 1m, 
                LastUpdated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) 
            },
            new MarketAsset 
            { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), 
                CategoryId = fiatCategoryId, 
                Symbol = "USD", 
                Name = "USD Cash", 
                CurrentPrice = 1m, 
                LastUpdated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) 
            }
        );

        // Seed Global Cashflow Categories
        modelBuilder.Entity<CashflowCategory>().HasData(
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0001-000000000001"), Name = "Lương", Type = CashflowType.Income, Icon = "💰", Color = "#10B981", IsGlobal = true, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0001-000000000002"), Name = "Thưởng", Type = CashflowType.Income, Icon = "🎁", Color = "#34D399", IsGlobal = true, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0001-000000000003"), Name = "Đầu tư", Type = CashflowType.Income, Icon = "📈", Color = "#059669", IsGlobal = true, UserId = null },
            
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0002-000000000001"), Name = "Ăn uống", Type = CashflowType.Expense, Icon = "🍔", Color = "#EF4444", IsGlobal = true, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0002-000000000002"), Name = "Tiền nhà", Type = CashflowType.Expense, Icon = "🏠", Color = "#F87171", IsGlobal = true, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0002-000000000003"), Name = "Đi lại", Type = CashflowType.Expense, Icon = "🚗", Color = "#FCA5A5", IsGlobal = true, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0002-000000000004"), Name = "Giải trí", Type = CashflowType.Expense, Icon = "🎮", Color = "#B91C1C", IsGlobal = true, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0002-000000000005"), Name = "Mua sắm", Type = CashflowType.Expense, Icon = "🛍️", Color = "#DC2626", IsGlobal = true, UserId = null }
        );
    }
}
