namespace FinanceTracker.Web.Services;

using FinanceTracker.Web.Database;
using FinanceTracker.Web.Database.Entities;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// PostgreSQL-backed implementation of IPortfolioRepository using EF Core.
/// Uses a delete-then-insert strategy within a transaction to mirror the full-replace
/// semantics of the JSON file implementation. Suitable for the app's usage patterns
/// (infrequent writes, always full portfolio state available).
/// Register this instead of JsonPortfolioRepository when Storage:UseDatabase = true.
/// </summary>
public class PostgresPortfolioRepository : IPortfolioRepository
{
    private readonly IDbContextFactory<FinanceTrackerDbContext> _dbFactory;
    private readonly ILogger<PostgresPortfolioRepository> _logger;

    public PostgresPortfolioRepository(IDbContextFactory<FinanceTrackerDbContext> dbFactory, ILogger<PostgresPortfolioRepository> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<UserPortfolio> GetPortfolioAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return new UserPortfolio { UserId = userId };

        await using var db = await _dbFactory.CreateDbContextAsync();

        var equities = await db.EquityHoldings.AsNoTracking().Where(h => h.UserId == userGuid).ToListAsync();
        var mfs = await db.MutualFundHoldings.AsNoTracking().Where(h => h.UserId == userGuid).ToListAsync();
        var cash = await db.CashHoldings.AsNoTracking().Where(h => h.UserId == userGuid).ToListAsync();
        var usStocks = await db.UsStockHoldings.AsNoTracking().Where(h => h.UserId == userGuid).ToListAsync();
        var expenses = await db.Expenses.AsNoTracking().Where(e => e.UserId == userGuid).ToListAsync();
        var snapshots = await db.MonthlySnapshots.AsNoTracking().Where(s => s.UserId == userGuid).ToListAsync();

        _logger.LogDebug("Loaded portfolio from Postgres for user {UserId}", userId);

        return new UserPortfolio
        {
            UserId = userId,
            LastUpdated = DateTime.UtcNow,
            EquityHoldings = equities.Select(MapEquity).ToList(),
            MutualFundHoldings = mfs.Select(MapMf).ToList(),
            CashHoldings = cash.Select(MapCash).ToList(),
            UsStockHoldings = usStocks.Select(MapUsStock).ToList(),
            Expenses = expenses.Select(MapExpense).ToList(),
            MonthlySnapshots = snapshots.Select(MapSnapshot).ToList()
        };
    }

    public async Task SavePortfolioAsync(UserPortfolio portfolio)
    {
        if (!Guid.TryParse(portfolio.UserId, out var userGuid))
        {
            _logger.LogError("Invalid userId '{UserId}' — cannot save portfolio to Postgres", portfolio.UserId);
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            // Delete all existing rows for this user
            await db.EquityHoldings.Where(h => h.UserId == userGuid).ExecuteDeleteAsync();
            await db.MutualFundHoldings.Where(h => h.UserId == userGuid).ExecuteDeleteAsync();
            await db.CashHoldings.Where(h => h.UserId == userGuid).ExecuteDeleteAsync();
            await db.UsStockHoldings.Where(h => h.UserId == userGuid).ExecuteDeleteAsync();
            await db.Expenses.Where(e => e.UserId == userGuid).ExecuteDeleteAsync();
            await db.MonthlySnapshots.Where(s => s.UserId == userGuid).ExecuteDeleteAsync();

            // Insert current state
            db.EquityHoldings.AddRange(portfolio.EquityHoldings.Select(h => MapEquityEntity(h, userGuid)));
            db.MutualFundHoldings.AddRange(portfolio.MutualFundHoldings.Select(h => MapMfEntity(h, userGuid)));
            db.CashHoldings.AddRange(portfolio.CashHoldings.Select(h => MapCashEntity(h, userGuid)));
            db.UsStockHoldings.AddRange(portfolio.UsStockHoldings.Select(h => MapUsStockEntity(h, userGuid)));
            db.Expenses.AddRange(portfolio.Expenses.Select(e => MapExpenseEntity(e, userGuid)));
            db.MonthlySnapshots.AddRange(portfolio.MonthlySnapshots.Select(s => MapSnapshotEntity(s, userGuid)));

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogDebug("Saved portfolio to Postgres for user {UserId}", portfolio.UserId);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Failed to save portfolio to Postgres for user {UserId}", portfolio.UserId);
            throw;
        }
    }

    // ── Domain → Entity mappers ───────────────────────────────────────────────

    private static EquityHoldingEntity MapEquityEntity(EquityHolding h, Guid userId) => new()
    {
        Id = Guid.TryParse(h.Id, out var g) ? g : Guid.NewGuid(),
        UserId = userId,
        AccountTag = h.AccountTag,
        Symbol = h.Symbol,
        Isin = h.Isin,
        Exchange = h.Exchange,
        CompanyName = h.CompanyName,
        Quantity = h.Quantity,
        AverageBuyPrice = h.AverageBuyPrice,
        CurrentPrice = h.CurrentPrice,
        LastPriceUpdate = h.LastPriceUpdate,
        AddedAt = h.AddedAt
    };

    private static MutualFundHoldingEntity MapMfEntity(MutualFundHolding h, Guid userId) => new()
    {
        Id = Guid.TryParse(h.Id, out var g) ? g : Guid.NewGuid(),
        UserId = userId,
        AccountTag = h.AccountTag,
        SchemeCode = h.SchemeCode,
        SchemeName = h.SchemeName,
        Amc = h.Amc,
        Category = h.Category,
        FolioNumber = h.FolioNumber,
        Units = h.Units,
        AverageNav = h.AverageNav,
        CurrentNav = h.CurrentNav,
        LastNavUpdate = h.LastNavUpdate,
        AddedAt = h.AddedAt
    };

    private static CashHoldingEntity MapCashEntity(CashHolding h, Guid userId) => new()
    {
        Id = Guid.TryParse(h.Id, out var g) ? g : Guid.NewGuid(),
        UserId = userId,
        BankName = h.BankName,
        AccountType = h.AccountType,
        Balance = h.Balance,
        LastUpdated = h.LastUpdated
    };

    private static UsStockHoldingEntity MapUsStockEntity(UsStockHolding h, Guid userId) => new()
    {
        Id = Guid.TryParse(h.Id, out var g) ? g : Guid.NewGuid(),
        UserId = userId,
        Symbol = h.Symbol,
        CompanyName = h.CompanyName,
        Quantity = h.Quantity,
        AverageBuyPriceUsd = h.AverageBuyPriceUsd,
        CurrentPriceUsd = h.CurrentPriceUsd,
        ExchangeRateUsdInr = h.ExchangeRateUsdInr,
        LastPriceUpdate = h.LastPriceUpdate,
        AddedAt = h.AddedAt
    };

    private static ExpenseEntity MapExpenseEntity(Expense e, Guid userId) => new()
    {
        Id = Guid.TryParse(e.Id, out var g) ? g : Guid.NewGuid(),
        UserId = userId,
        Date = e.Date,
        Amount = e.Amount,
        Category = e.Category,
        Description = e.Description,
        Tags = e.Tags.ToArray(),
        Source = e.Source,
        AddedAt = e.AddedAt
    };

    private static MonthlySnapshotEntity MapSnapshotEntity(MonthlySnapshot s, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Month = s.Month,
        TotalInvested = s.TotalInvested,
        TotalCurrentValue = s.TotalCurrentValue,
        EquityValue = s.EquityValue,
        MutualFundValue = s.MutualFundValue,
        CashValue = s.CashValue,
        UsStockValue = s.UsStockValue,
        SnapshotDate = s.SnapshotDate
    };

    // ── Entity → Domain mappers ───────────────────────────────────────────────

    private static EquityHolding MapEquity(EquityHoldingEntity e) => new()
    {
        Id = e.Id.ToString(),
        AccountTag = e.AccountTag,
        Symbol = e.Symbol,
        Isin = e.Isin,
        Exchange = e.Exchange,
        CompanyName = e.CompanyName,
        Quantity = e.Quantity,
        AverageBuyPrice = e.AverageBuyPrice,
        CurrentPrice = e.CurrentPrice,
        LastPriceUpdate = e.LastPriceUpdate,
        AddedAt = e.AddedAt
    };

    private static MutualFundHolding MapMf(MutualFundHoldingEntity e) => new()
    {
        Id = e.Id.ToString(),
        AccountTag = e.AccountTag,
        SchemeCode = e.SchemeCode,
        SchemeName = e.SchemeName,
        Amc = e.Amc,
        Category = e.Category,
        FolioNumber = e.FolioNumber,
        Units = e.Units,
        AverageNav = e.AverageNav,
        CurrentNav = e.CurrentNav,
        LastNavUpdate = e.LastNavUpdate,
        AddedAt = e.AddedAt
    };

    private static CashHolding MapCash(CashHoldingEntity e) => new()
    {
        Id = e.Id.ToString(),
        BankName = e.BankName,
        AccountType = e.AccountType,
        Balance = e.Balance,
        LastUpdated = e.LastUpdated
    };

    private static UsStockHolding MapUsStock(UsStockHoldingEntity e) => new()
    {
        Id = e.Id.ToString(),
        Symbol = e.Symbol,
        CompanyName = e.CompanyName,
        Quantity = e.Quantity,
        AverageBuyPriceUsd = e.AverageBuyPriceUsd,
        CurrentPriceUsd = e.CurrentPriceUsd,
        ExchangeRateUsdInr = e.ExchangeRateUsdInr,
        LastPriceUpdate = e.LastPriceUpdate,
        AddedAt = e.AddedAt
    };

    private static Expense MapExpense(ExpenseEntity e) => new()
    {
        Id = e.Id.ToString(),
        Date = e.Date,
        Amount = e.Amount,
        Category = e.Category,
        Description = e.Description,
        Tags = e.Tags.ToList(),
        Source = e.Source,
        AddedAt = e.AddedAt
    };

    private static MonthlySnapshot MapSnapshot(MonthlySnapshotEntity e) => new()
    {
        Month = e.Month,
        TotalInvested = e.TotalInvested,
        TotalCurrentValue = e.TotalCurrentValue,
        EquityValue = e.EquityValue,
        MutualFundValue = e.MutualFundValue,
        CashValue = e.CashValue,
        UsStockValue = e.UsStockValue,
        SnapshotDate = e.SnapshotDate
    };
}
