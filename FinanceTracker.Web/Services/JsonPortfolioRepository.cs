namespace FinanceTracker.Web.Services;

using System.Text.Json;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;

public class JsonPortfolioRepository : IPortfolioRepository
{
    private readonly string _portfoliosDir;
    private readonly ILogger<JsonPortfolioRepository> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonPortfolioRepository(IWebHostEnvironment env, ILogger<JsonPortfolioRepository> logger)
    {
        _logger = logger;
        _portfoliosDir = Path.Combine(env.ContentRootPath, "Data", "portfolios");
        Directory.CreateDirectory(_portfoliosDir);
        _logger.LogInformation("PortfolioRepository initialized, data dir: {Dir}", _portfoliosDir);
    }

    private string GetFilePath(string userId) => Path.Combine(_portfoliosDir, $"{userId}.json");

    public async Task<UserPortfolio> GetPortfolioAsync(string userId)
    {
        var filePath = GetFilePath(userId);
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("No portfolio file for user {UserId}, returning empty portfolio", userId);
                return new UserPortfolio { UserId = userId };
            }
            var json = await File.ReadAllTextAsync(filePath);
            _logger.LogDebug("Loaded portfolio for user {UserId} from {FilePath}", userId, filePath);
            return JsonSerializer.Deserialize<UserPortfolio>(json, _jsonOptions)
                   ?? new UserPortfolio { UserId = userId };
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SavePortfolioAsync(UserPortfolio portfolio)
    {
        var filePath = GetFilePath(portfolio.UserId);
        await _lock.WaitAsync();
        try
        {
            portfolio.LastUpdated = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(portfolio, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            _logger.LogDebug("Saved portfolio for user {UserId}: {EquityCount} equities, {MFCount} MFs, {CashCount} cash",
                portfolio.UserId, portfolio.EquityHoldings.Count, portfolio.MutualFundHoldings.Count, portfolio.CashHoldings.Count);
        }
        finally
        {
            _lock.Release();
        }
    }
}
