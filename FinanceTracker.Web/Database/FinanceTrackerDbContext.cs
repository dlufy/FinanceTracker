namespace FinanceTracker.Web.Database;

using FinanceTracker.Web.Database.Entities;
using Microsoft.EntityFrameworkCore;

public class FinanceTrackerDbContext : DbContext
{
    public FinanceTrackerDbContext(DbContextOptions<FinanceTrackerDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<EquityHoldingEntity> EquityHoldings => Set<EquityHoldingEntity>();
    public DbSet<MutualFundHoldingEntity> MutualFundHoldings => Set<MutualFundHoldingEntity>();
    public DbSet<CashHoldingEntity> CashHoldings => Set<CashHoldingEntity>();
    public DbSet<UsStockHoldingEntity> UsStockHoldings => Set<UsStockHoldingEntity>();
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    public DbSet<MonthlySnapshotEntity> MonthlySnapshots => Set<MonthlySnapshotEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Users ────────────────────────────────────────────────────────────
        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.UserName).HasColumnName("user_name").HasMaxLength(100).IsRequired();
            e.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            e.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(u => u.CreatedAt).HasColumnName("created_at");
            e.HasIndex(u => u.UserName).IsUnique();
        });

        // ── Equity Holdings ──────────────────────────────────────────────────
        modelBuilder.Entity<EquityHoldingEntity>(e =>
        {
            e.ToTable("equity_holdings");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("id");
            e.Property(h => h.UserId).HasColumnName("user_id");
            e.Property(h => h.AccountTag).HasColumnName("account_tag").HasMaxLength(100).IsRequired();
            e.Property(h => h.Symbol).HasColumnName("symbol").HasMaxLength(50).IsRequired();
            e.Property(h => h.Isin).HasColumnName("isin").HasMaxLength(20);
            e.Property(h => h.Exchange).HasColumnName("exchange").HasMaxLength(10).HasDefaultValue("NSE");
            e.Property(h => h.CompanyName).HasColumnName("company_name").HasMaxLength(300);
            e.Property(h => h.Quantity).HasColumnName("quantity");
            e.Property(h => h.AverageBuyPrice).HasColumnName("average_buy_price").HasColumnType("numeric(18,4)");
            e.Property(h => h.CurrentPrice).HasColumnName("current_price").HasColumnType("numeric(18,4)");
            e.Property(h => h.LastPriceUpdate).HasColumnName("last_price_update");
            e.Property(h => h.AddedAt).HasColumnName("added_at");
            e.HasOne(h => h.User).WithMany(u => u.EquityHoldings).HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(h => h.UserId);
            e.HasIndex(h => new { h.UserId, h.AccountTag });
        });

        // ── Mutual Fund Holdings ─────────────────────────────────────────────
        modelBuilder.Entity<MutualFundHoldingEntity>(e =>
        {
            e.ToTable("mutual_fund_holdings");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("id");
            e.Property(h => h.UserId).HasColumnName("user_id");
            e.Property(h => h.AccountTag).HasColumnName("account_tag").HasMaxLength(100).IsRequired();
            e.Property(h => h.SchemeCode).HasColumnName("scheme_code").HasMaxLength(20);
            e.Property(h => h.SchemeName).HasColumnName("scheme_name").HasMaxLength(500).IsRequired();
            e.Property(h => h.Amc).HasColumnName("amc").HasMaxLength(200);
            e.Property(h => h.Category).HasColumnName("category").HasMaxLength(100);
            e.Property(h => h.FolioNumber).HasColumnName("folio_number").HasMaxLength(50);
            e.Property(h => h.Units).HasColumnName("units").HasColumnType("numeric(18,6)");
            e.Property(h => h.AverageNav).HasColumnName("average_nav").HasColumnType("numeric(18,6)");
            e.Property(h => h.CurrentNav).HasColumnName("current_nav").HasColumnType("numeric(18,6)");
            e.Property(h => h.LastNavUpdate).HasColumnName("last_nav_update");
            e.Property(h => h.AddedAt).HasColumnName("added_at");
            e.HasOne(h => h.User).WithMany(u => u.MutualFundHoldings).HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(h => h.UserId);
            e.HasIndex(h => new { h.UserId, h.AccountTag });
        });

        // ── Cash Holdings ────────────────────────────────────────────────────
        modelBuilder.Entity<CashHoldingEntity>(e =>
        {
            e.ToTable("cash_holdings");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("id");
            e.Property(h => h.UserId).HasColumnName("user_id");
            e.Property(h => h.BankName).HasColumnName("bank_name").HasMaxLength(200).IsRequired();
            e.Property(h => h.AccountType).HasColumnName("account_type").HasMaxLength(50).HasDefaultValue("Savings");
            e.Property(h => h.Balance).HasColumnName("balance").HasColumnType("numeric(18,2)");
            e.Property(h => h.LastUpdated).HasColumnName("last_updated");
            e.HasOne(h => h.User).WithMany(u => u.CashHoldings).HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(h => h.UserId);
        });

        // ── US Stock Holdings ─────────────────────────────────────────────────
        modelBuilder.Entity<UsStockHoldingEntity>(e =>
        {
            e.ToTable("us_stock_holdings");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("id");
            e.Property(h => h.UserId).HasColumnName("user_id");
            e.Property(h => h.Symbol).HasColumnName("symbol").HasMaxLength(20).IsRequired();
            e.Property(h => h.CompanyName).HasColumnName("company_name").HasMaxLength(300);
            e.Property(h => h.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,6)");
            e.Property(h => h.AverageBuyPriceUsd).HasColumnName("average_buy_price_usd").HasColumnType("numeric(18,4)");
            e.Property(h => h.CurrentPriceUsd).HasColumnName("current_price_usd").HasColumnType("numeric(18,4)");
            e.Property(h => h.ExchangeRateUsdInr).HasColumnName("exchange_rate_usd_inr").HasColumnType("numeric(10,4)");
            e.Property(h => h.LastPriceUpdate).HasColumnName("last_price_update");
            e.Property(h => h.AddedAt).HasColumnName("added_at");
            e.HasOne(h => h.User).WithMany(u => u.UsStockHoldings).HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(h => h.UserId);
        });

        // ── Expenses ──────────────────────────────────────────────────────────
        modelBuilder.Entity<ExpenseEntity>(e =>
        {
            e.ToTable("expenses");
            e.HasKey(ex => ex.Id);
            e.Property(ex => ex.Id).HasColumnName("id");
            e.Property(ex => ex.UserId).HasColumnName("user_id");
            e.Property(ex => ex.Date).HasColumnName("date");
            e.Property(ex => ex.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            e.Property(ex => ex.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
            e.Property(ex => ex.Description).HasColumnName("description").HasMaxLength(500);
            e.Property(ex => ex.Tags).HasColumnName("tags").HasColumnType("text[]");
            e.Property(ex => ex.Source).HasColumnName("source").HasMaxLength(20).HasDefaultValue("Manual");
            e.Property(ex => ex.AddedAt).HasColumnName("added_at");
            e.HasOne(ex => ex.User).WithMany(u => u.Expenses).HasForeignKey(ex => ex.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(ex => ex.UserId);
            e.HasIndex(ex => new { ex.UserId, ex.Date });
        });

        // ── Monthly Snapshots ─────────────────────────────────────────────────
        modelBuilder.Entity<MonthlySnapshotEntity>(e =>
        {
            e.ToTable("monthly_snapshots");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.UserId).HasColumnName("user_id");
            e.Property(s => s.Month).HasColumnName("month").HasMaxLength(7).IsRequired(); // "2026-03"
            e.Property(s => s.TotalInvested).HasColumnName("total_invested").HasColumnType("numeric(18,2)");
            e.Property(s => s.TotalCurrentValue).HasColumnName("total_current_value").HasColumnType("numeric(18,2)");
            e.Property(s => s.EquityValue).HasColumnName("equity_value").HasColumnType("numeric(18,2)");
            e.Property(s => s.MutualFundValue).HasColumnName("mutual_fund_value").HasColumnType("numeric(18,2)");
            e.Property(s => s.CashValue).HasColumnName("cash_value").HasColumnType("numeric(18,2)");
            e.Property(s => s.UsStockValue).HasColumnName("us_stock_value").HasColumnType("numeric(18,2)");
            e.Property(s => s.SnapshotDate).HasColumnName("snapshot_date");
            e.HasOne(s => s.User).WithMany(u => u.MonthlySnapshots).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.UserId);
            e.HasIndex(s => new { s.UserId, s.Month }).IsUnique();
        });
    }
}
