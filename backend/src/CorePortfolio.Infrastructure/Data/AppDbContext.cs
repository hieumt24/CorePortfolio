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
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<TargetAllocation> TargetAllocations => Set<TargetAllocation>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<CashAccount> CashAccounts => Set<CashAccount>();
    public DbSet<CashLedgerEntry> CashLedgerEntries => Set<CashLedgerEntry>();
    public DbSet<SavingGoal> SavingGoals => Set<SavingGoal>();
    public DbSet<DcaPlan> DcaPlans => Set<DcaPlan>();
    public DbSet<RebalanceExecutionPlan> RebalanceExecutionPlans => Set<RebalanceExecutionPlan>();
    public DbSet<RebalanceExecutionPlanItem> RebalanceExecutionPlanItems => Set<RebalanceExecutionPlanItem>();
    public DbSet<RecurringCashflowRule> RecurringCashflowRules => Set<RecurringCashflowRule>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<BenchmarkDefinition> BenchmarkDefinitions => Set<BenchmarkDefinition>();
    public DbSet<BenchmarkPricePoint> BenchmarkPricePoints => Set<BenchmarkPricePoint>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<SessionRefreshToken> SessionRefreshTokens => Set<SessionRefreshToken>();
    public DbSet<TwoFactorChallenge> TwoFactorChallenges => Set<TwoFactorChallenge>();
    public DbSet<TwoFactorRecoveryCode> TwoFactorRecoveryCodes => Set<TwoFactorRecoveryCode>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AdvanceConcurrencyVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AdvanceConcurrencyVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AdvanceConcurrencyVersions()
    {
        foreach (var entry in ChangeTracker.Entries<IConcurrencyTracked>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Version < 1)
                entry.Entity.Version = 1;
            else if (entry.State == EntityState.Modified)
                entry.Entity.Version++;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.Username)
            .HasMaxLength(50);

        modelBuilder.Entity<User>()
            .Property(u => u.DisplayName)
            .HasMaxLength(80);

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .HasMaxLength(160);

        modelBuilder.Entity<User>()
            .Property(u => u.LastLoginIpAddress)
            .HasMaxLength(45);

        modelBuilder.Entity<User>()
            .Property(u => u.TwoFactorSecretEncrypted)
            .HasMaxLength(500);

        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.Role, u.IsActive });

        modelBuilder.Entity<User>()
            .HasIndex(u => u.LastActivityAt);

        modelBuilder.Entity<UserSession>(session =>
        {
            session.Property(item => item.TokenId).HasMaxLength(100);
            session.Property(item => item.IpAddress).HasMaxLength(45);
            session.Property(item => item.UserAgent).HasMaxLength(500);
            session.Property(item => item.RevokeReason).HasMaxLength(250);
            session.Property(item => item.AuthenticationMethod).HasMaxLength(30);
            session.HasIndex(item => item.TokenId).IsUnique();
            session.HasIndex(item => new { item.UserId, item.RevokedAt, item.ExpiresAt });
            session.HasOne(item => item.User)
                .WithMany(item => item.Sessions)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionRefreshToken>(refreshToken =>
        {
            refreshToken.Property(item => item.TokenHash).HasMaxLength(64);
            refreshToken.HasIndex(item => item.TokenHash).IsUnique();
            refreshToken.HasIndex(item => new { item.UserSessionId, item.ExpiresAt });
            refreshToken.HasOne(item => item.UserSession)
                .WithMany(item => item.RefreshTokens)
                .HasForeignKey(item => item.UserSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TwoFactorChallenge>(challenge =>
        {
            challenge.Property(item => item.TokenHash).HasMaxLength(64);
            challenge.Property(item => item.Purpose).HasConversion<string>().HasMaxLength(30);
            challenge.Property(item => item.PendingSecretEncrypted).HasMaxLength(500);
            challenge.Property(item => item.IpAddress).HasMaxLength(45);
            challenge.Property(item => item.UserAgent).HasMaxLength(500);
            challenge.HasIndex(item => item.TokenHash).IsUnique();
            challenge.HasIndex(item => new { item.UserId, item.ExpiresAt, item.ConsumedAt });
            challenge.HasOne(item => item.User)
                .WithMany(item => item.TwoFactorChallenges)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TwoFactorRecoveryCode>(recoveryCode =>
        {
            recoveryCode.Property(item => item.CodeHash).HasMaxLength(64);
            recoveryCode.HasIndex(item => item.CodeHash).IsUnique();
            recoveryCode.HasIndex(item => new { item.UserId, item.UsedAt });
            recoveryCode.HasOne(item => item.User)
                .WithMany(item => item.TwoFactorRecoveryCodes)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        foreach (var entityType in new[]
        {
            typeof(MarketAsset),
            typeof(Budget),
            typeof(SavingGoal),
            typeof(DcaPlan),
            typeof(RebalanceExecutionPlan),
            typeof(NotificationPreference)
        })
        {
            modelBuilder.Entity(entityType)
                .Property(nameof(IConcurrencyTracked.Version))
                .IsConcurrencyToken()
                .HasDefaultValue(1);
        }

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.Property(item => item.Action).HasMaxLength(100);
            entity.Property(item => item.EntityType).HasMaxLength(100);
            entity.Property(item => item.EntityId).HasMaxLength(100);
            entity.Property(item => item.Outcome).HasMaxLength(30);
            entity.Property(item => item.IpAddress).HasMaxLength(45);
            entity.Property(item => item.CorrelationId).HasMaxLength(100);
            entity.Property(item => item.MetadataJson).HasMaxLength(4000);
            entity.HasIndex(item => item.OccurredAt);
            entity.HasIndex(item => new { item.ActorUserId, item.OccurredAt });
            entity.HasIndex(item => new { item.Action, item.OccurredAt });
        });
        
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

        modelBuilder.Entity<Portfolio>()
            .HasMany(p => p.CashAccounts)
            .WithOne(c => c.Portfolio)
            .HasForeignKey(c => c.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CashAccount>()
            .HasIndex(c => new { c.PortfolioId, c.Currency })
            .IsUnique();

        modelBuilder.Entity<CashLedgerEntry>()
            .HasOne(e => e.CashAccount)
            .WithMany(a => a.Entries)
            .HasForeignKey(e => e.CashAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CashLedgerEntry>()
            .HasOne(e => e.Transaction)
            .WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CashLedgerEntry>()
            .HasIndex(e => e.TransactionId)
            .IsUnique();

        modelBuilder.Entity<CashLedgerEntry>()
            .HasIndex(e => new { e.CashAccountId, e.Classification, e.OccurredAt });

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

        modelBuilder.Entity<User>()
            .HasMany(u => u.WatchlistItems)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.TargetAllocations)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TargetAllocation>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PortfolioSnapshot>()
            .HasIndex(snapshot => new { snapshot.PortfolioId, snapshot.Date })
            .IsUnique();

        modelBuilder.Entity<BenchmarkDefinition>(benchmark =>
        {
            benchmark.Property(item => item.Name).HasMaxLength(100);
            benchmark.Property(item => item.Symbol).HasMaxLength(30);
            benchmark.Property(item => item.AssetGroup).HasMaxLength(20);
            benchmark.Property(item => item.Currency).HasMaxLength(3);
            benchmark.HasIndex(item => new { item.AssetGroup, item.IsActive, item.IsDefault });
            benchmark.HasOne(item => item.MarketAsset)
                .WithMany()
                .HasForeignKey(item => item.MarketAssetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BenchmarkPricePoint>(pricePoint =>
        {
            pricePoint.Property(item => item.Currency).HasMaxLength(3);
            pricePoint.Property(item => item.Source).HasMaxLength(40);
            pricePoint.Property(item => item.QualityStatus).HasMaxLength(30);
            pricePoint.HasIndex(item => new { item.BenchmarkDefinitionId, item.Date })
                .IsUnique();
            pricePoint.HasOne(item => item.BenchmarkDefinition)
                .WithMany(item => item.PricePoints)
                .HasForeignKey(item => item.BenchmarkDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BenchmarkDefinition>().HasData(
            new BenchmarkDefinition
            {
                Id = Guid.Parse("00000000-0000-0000-0005-000000000001"),
                Name = "VN-Index",
                Symbol = "VNINDEX",
                AssetGroup = "Stock",
                IsDefault = true,
                Currency = "VND",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new BenchmarkDefinition
            {
                Id = Guid.Parse("00000000-0000-0000-0005-000000000002"),
                Name = "Bitcoin",
                Symbol = "BTC",
                AssetGroup = "Crypto",
                IsDefault = true,
                Currency = "USD",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<Budget>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SavingGoal>()
            .HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SavingGoal>()
            .HasOne(g => g.Portfolio)
            .WithMany()
            .HasForeignKey(g => g.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SavingGoal>()
            .HasOne(g => g.CashAccount)
            .WithMany()
            .HasForeignKey(g => g.CashAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SavingGoal>()
            .HasOne(g => g.CashflowCategory)
            .WithMany()
            .HasForeignKey(g => g.CashflowCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DcaPlan>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DcaPlan>()
            .HasOne(p => p.Portfolio)
            .WithMany()
            .HasForeignKey(p => p.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DcaPlan>()
            .HasOne(p => p.MarketAsset)
            .WithMany()
            .HasForeignKey(p => p.MarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RebalanceExecutionPlan>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RebalanceExecutionPlanItem>()
            .HasOne(i => i.Plan)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RebalanceExecutionPlanItem>()
            .HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecurringCashflowRule>().HasIndex(r => new { r.UserId, r.NextOccurrence });
        modelBuilder.Entity<RecurringCashflowRule>().HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecurringCashflowRule>().HasOne(r => r.Portfolio).WithMany().HasForeignKey(r => r.PortfolioId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecurringCashflowRule>().HasOne(r => r.Category).WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Notification>(notification =>
        {
            notification.Property(n => n.Type).HasConversion<string>();
            notification.Property(n => n.Severity).HasConversion<string>();
            notification.Property(n => n.Title).HasMaxLength(160);
            notification.Property(n => n.Link).HasMaxLength(500);
            notification.Property(n => n.DedupeKey).HasMaxLength(300);
            notification.Property(n => n.EntityType).HasMaxLength(100);
            notification.Property(n => n.ActionLabel).HasMaxLength(80);
            notification.HasIndex(n => new { n.UserId, n.CreatedAt });
            notification.HasIndex(n => new { n.UserId, n.DedupeKey }).IsUnique();
            notification.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationPreference>(preference =>
        {
            preference.Property(p => p.Type).HasConversion<string>();
            preference.HasIndex(p => new { p.UserId, p.Type }).IsUnique();
            preference.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WatchlistItem>()
            .HasOne(w => w.MarketAsset)
            .WithMany()
            .HasForeignKey(w => w.MarketAssetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CashflowRecord>()
            .HasOne(c => c.Category)
            .WithMany()
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<CashflowCategory>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
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
        // --- INCOME ---
        var catIncomeLương = Guid.Parse("00000000-0000-0000-0001-000000000001"); // Old Lương
        var catIncomeThuNhapPhu = Guid.Parse("00000000-0000-0000-0001-000000000003"); // Old Đầu tư (repurposed to Thu nhập phụ since Đầu tư is now Investment)
        var catIncomeKhac = Guid.Parse("00000000-0000-0000-0001-A00000000001");

        // --- EXPENSE ---
        var catExpAnUong = Guid.Parse("00000000-0000-0000-0002-000000000001"); // Old Ăn uống
        var catExpNhaO = Guid.Parse("00000000-0000-0000-0001-A00000000002");
        var catExpDiLai = Guid.Parse("00000000-0000-0000-0002-000000000003"); // Old Đi lại (Di chuyển)
        var catExpSinhHoat = Guid.Parse("00000000-0000-0000-0001-A00000000003");
        var catExpQuanHe = Guid.Parse("00000000-0000-0000-0001-A00000000004");
        var catExpGiaiTri = Guid.Parse("00000000-0000-0000-0002-000000000004"); // Old Giải trí
        var catExpDuLich = Guid.Parse("00000000-0000-0000-0001-A00000000005");
        var catExpHocTap = Guid.Parse("00000000-0000-0000-0001-A00000000006");
        var catExpKhac = Guid.Parse("00000000-0000-0000-0001-A00000000007");

        // --- INVESTMENT ---
        var catInvDauTu = Guid.Parse("00000000-0000-0000-0003-000000000001");

        // --- SAVING ---
        var catSavTietKiem = Guid.Parse("00000000-0000-0000-0004-000000000001");

        modelBuilder.Entity<CashflowCategory>().HasData(
            // Income Parents
            new CashflowCategory { Id = catIncomeLương, Name = "Lương", Type = CashflowType.Income, Icon = "💵", Color = "#10B981", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("694f5dee-6171-4b45-be98-b12e46510a73"), ParentCategoryId = catIncomeLương, Name = "Lương chính", Type = CashflowType.Income, Icon = "💵", Color = "#10B981", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0001-000000000002"), ParentCategoryId = catIncomeLương, Name = "Thưởng", Type = CashflowType.Income, Icon = "🎁", Color = "#34D399", IsGlobal = true, SortOrder = 2, UserId = null }, // Old Thưởng
            new CashflowCategory { Id = Guid.Parse("dd537949-bade-4d30-ac2a-62b6710b0fa7"), ParentCategoryId = catIncomeLương, Name = "OT", Type = CashflowType.Income, Icon = "🕒", Color = "#6EE7B7", IsGlobal = true, SortOrder = 3, UserId = null },
            
            new CashflowCategory { Id = catIncomeThuNhapPhu, Name = "Thu nhập phụ", Type = CashflowType.Income, Icon = "💼", Color = "#059669", IsGlobal = true, SortOrder = 2, UserId = null },
            new CashflowCategory { Id = Guid.Parse("900175b4-e02d-4504-a20b-863206362b60"), ParentCategoryId = catIncomeThuNhapPhu, Name = "Freelance", Type = CashflowType.Income, Icon = "💻", Color = "#059669", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("3e25eb4b-adb7-4fbc-b8ad-bf13093056d3"), ParentCategoryId = catIncomeThuNhapPhu, Name = "Bán hàng online", Type = CashflowType.Income, Icon = "📦", Color = "#047857", IsGlobal = true, SortOrder = 2, UserId = null },

            new CashflowCategory { Id = catIncomeKhac, Name = "Khác", Type = CashflowType.Income, Icon = "🎁", Color = "#A7F3D0", IsGlobal = true, SortOrder = 3, UserId = null },

            // Expense Parents
            new CashflowCategory { Id = catExpAnUong, Name = "Ăn uống", Type = CashflowType.Expense, Icon = "🍜", Color = "#EF4444", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("6f8052ba-84d6-49db-b84a-2f8ef4643540"), ParentCategoryId = catExpAnUong, Name = "Hằng ngày", Type = CashflowType.Expense, Icon = "🍚", Color = "#EF4444", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("c34be7d4-b7cf-40b5-9120-adb21e7bb52a"), ParentCategoryId = catExpAnUong, Name = "Ăn ngoài", Type = CashflowType.Expense, Icon = "🍽️", Color = "#F87171", IsGlobal = true, SortOrder = 2, UserId = null },
            new CashflowCategory { Id = Guid.Parse("df4c8022-9f49-4beb-94aa-397650e13b57"), ParentCategoryId = catExpAnUong, Name = "Coffee/Trà sữa", Type = CashflowType.Expense, Icon = "☕", Color = "#FCA5A5", IsGlobal = true, SortOrder = 3, UserId = null },

            new CashflowCategory { Id = catExpNhaO, Name = "Chỗ ở & Cố định", Type = CashflowType.Expense, Icon = "🏠", Color = "#F97316", IsGlobal = true, SortOrder = 2, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0002-000000000002"), ParentCategoryId = catExpNhaO, Name = "Tiền nhà", Type = CashflowType.Expense, Icon = "🏘️", Color = "#F97316", IsGlobal = true, SortOrder = 1, UserId = null }, // Old Tiền nhà
            new CashflowCategory { Id = Guid.Parse("e45cb584-d6bf-44e0-beeb-5bc1cb3960cb"), ParentCategoryId = catExpNhaO, Name = "Điện", Type = CashflowType.Expense, Icon = "⚡", Color = "#FB923C", IsGlobal = true, SortOrder = 2, UserId = null },
            new CashflowCategory { Id = Guid.Parse("5fc975e0-42f1-4fe4-bd58-45d6853a54e5"), ParentCategoryId = catExpNhaO, Name = "Nước", Type = CashflowType.Expense, Icon = "💧", Color = "#FDBA74", IsGlobal = true, SortOrder = 3, UserId = null },
            new CashflowCategory { Id = Guid.Parse("66292db5-3c78-4892-ae54-b93e40d14a7b"), ParentCategoryId = catExpNhaO, Name = "Internet", Type = CashflowType.Expense, Icon = "🌐", Color = "#FFEDD5", IsGlobal = true, SortOrder = 4, UserId = null },

            new CashflowCategory { Id = catExpDiLai, Name = "Di chuyển", Type = CashflowType.Expense, Icon = "🚗", Color = "#EAB308", IsGlobal = true, SortOrder = 3, UserId = null },
            new CashflowCategory { Id = Guid.Parse("8ab71e3f-24bf-498f-b04b-4b053c2a0b3a"), ParentCategoryId = catExpDiLai, Name = "Xăng xe", Type = CashflowType.Expense, Icon = "⛽", Color = "#EAB308", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("1e3efabd-a3bd-4584-82fd-122d282fa63d"), ParentCategoryId = catExpDiLai, Name = "Grab/Taxi", Type = CashflowType.Expense, Icon = "🚕", Color = "#FACC15", IsGlobal = true, SortOrder = 2, UserId = null },
            new CashflowCategory { Id = Guid.Parse("61930dcc-89b9-43db-a3fb-8d17279be178"), ParentCategoryId = catExpDiLai, Name = "Bảo dưỡng xe", Type = CashflowType.Expense, Icon = "🔧", Color = "#FEF08A", IsGlobal = true, SortOrder = 3, UserId = null },

            new CashflowCategory { Id = catExpSinhHoat, Name = "Sinh hoạt & Cá nhân", Type = CashflowType.Expense, Icon = "🧴", Color = "#3B82F6", IsGlobal = true, SortOrder = 4, UserId = null },
            new CashflowCategory { Id = Guid.Parse("3b4c2644-2aac-4eee-b147-a24f15369ab2"), ParentCategoryId = catExpSinhHoat, Name = "Đồ dùng sinh hoạt", Type = CashflowType.Expense, Icon = "🛒", Color = "#3B82F6", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("a53443b6-aeb0-4b3b-8e41-215ec23acd4a"), ParentCategoryId = catExpSinhHoat, Name = "Quần áo", Type = CashflowType.Expense, Icon = "👕", Color = "#60A5FA", IsGlobal = true, SortOrder = 2, UserId = null },
            new CashflowCategory { Id = Guid.Parse("fb25f644-e903-47de-a5a6-e4f2018e4a4f"), ParentCategoryId = catExpSinhHoat, Name = "Cắt tóc", Type = CashflowType.Expense, Icon = "✂️", Color = "#93C5FD", IsGlobal = true, SortOrder = 3, UserId = null },
            new CashflowCategory { Id = Guid.Parse("4b31b1d3-e5e3-4423-8ce2-03a0b2c707e0"), ParentCategoryId = catExpSinhHoat, Name = "Y tế", Type = CashflowType.Expense, Icon = "💊", Color = "#BFDBFE", IsGlobal = true, SortOrder = 4, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00000000-0000-0000-0002-000000000005"), ParentCategoryId = catExpSinhHoat, Name = "Mua sắm", Type = CashflowType.Expense, Icon = "🛍️", Color = "#DC2626", IsGlobal = true, SortOrder = 5, UserId = null }, // Old Mua sắm

            new CashflowCategory { Id = catExpQuanHe, Name = "Quan hệ xã hội", Type = CashflowType.Expense, Icon = "🎁", Color = "#EC4899", IsGlobal = true, SortOrder = 5, UserId = null },
            new CashflowCategory { Id = Guid.Parse("c50f14de-c0cc-4de1-926a-40252384f0b0"), ParentCategoryId = catExpQuanHe, Name = "Hiếu hỉ", Type = CashflowType.Expense, Icon = "💌", Color = "#EC4899", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("af1f5f06-79b7-4888-80cd-86d4695a8072"), ParentCategoryId = catExpQuanHe, Name = "Quà tặng", Type = CashflowType.Expense, Icon = "🎁", Color = "#F472B6", IsGlobal = true, SortOrder = 2, UserId = null },

            new CashflowCategory { Id = catExpGiaiTri, Name = "Giải trí", Type = CashflowType.Expense, Icon = "🎭", Color = "#8B5CF6", IsGlobal = true, SortOrder = 6, UserId = null },
            new CashflowCategory { Id = Guid.Parse("3ec84eb1-f674-4db2-acae-5b95b5552c63"), ParentCategoryId = catExpGiaiTri, Name = "Xem phim", Type = CashflowType.Expense, Icon = "🎬", Color = "#8B5CF6", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("b46a571f-7c9d-4b73-b3a3-da32fb178a5e"), ParentCategoryId = catExpGiaiTri, Name = "Game/Sub", Type = CashflowType.Expense, Icon = "🎮", Color = "#A78BFA", IsGlobal = true, SortOrder = 2, UserId = null },

            new CashflowCategory { Id = catExpDuLich, Name = "Du lịch", Type = CashflowType.Expense, Icon = "✈️", Color = "#06B6D4", IsGlobal = true, SortOrder = 7, UserId = null },
            
            new CashflowCategory { Id = catExpHocTap, Name = "Học tập", Type = CashflowType.Expense, Icon = "📚", Color = "#14B8A6", IsGlobal = true, SortOrder = 8, UserId = null },

            new CashflowCategory { Id = catExpKhac, Name = "Khác", Type = CashflowType.Expense, Icon = "❓", Color = "#64748B", IsGlobal = true, SortOrder = 9, UserId = null },

            // Investment
            new CashflowCategory { Id = catInvDauTu, Name = "Đầu tư", Type = CashflowType.Investment, Icon = "📈", Color = "#8B5CF6", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("773de4db-23b8-41b3-b752-140e3fc71a23"), ParentCategoryId = catInvDauTu, Name = "Crypto", Type = CashflowType.Investment, Icon = "₿", Color = "#8B5CF6", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("b6820223-9133-4b7c-b77e-bc17b40df075"), ParentCategoryId = catInvDauTu, Name = "Cổ phiếu", Type = CashflowType.Investment, Icon = "📊", Color = "#A78BFA", IsGlobal = true, SortOrder = 2, UserId = null },
            new CashflowCategory { Id = Guid.Parse("ccdaf802-c248-439f-9d8a-7c22ee25e3d8"), ParentCategoryId = catInvDauTu, Name = "Chứng chỉ quỹ", Type = CashflowType.Investment, Icon = "🏦", Color = "#C4B5FD", IsGlobal = true, SortOrder = 3, UserId = null },

            // Saving
            new CashflowCategory { Id = catSavTietKiem, Name = "Tiết kiệm", Type = CashflowType.Saving, Icon = "💰", Color = "#0EA5E9", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("fcae34ef-35d9-4cfa-beb1-2cc6838f6bd9"), ParentCategoryId = catSavTietKiem, Name = "Quỹ khẩn cấp", Type = CashflowType.Saving, Icon = "🛡️", Color = "#0EA5E9", IsGlobal = true, SortOrder = 1, UserId = null },
            new CashflowCategory { Id = Guid.Parse("00fc27d6-be2a-4cfb-99aa-5ae00b0d4617"), ParentCategoryId = catSavTietKiem, Name = "Gửi ngân hàng", Type = CashflowType.Saving, Icon = "🏦", Color = "#38BDF8", IsGlobal = true, SortOrder = 2, UserId = null }
        );
    }
}
