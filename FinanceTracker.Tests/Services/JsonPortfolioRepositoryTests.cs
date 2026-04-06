using System.Text.Json;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinanceTracker.Tests.Services;

public class JsonPortfolioRepositoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonPortfolioRepository _repo;

    public JsonPortfolioRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ft_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_tempDir);

        _repo = new JsonPortfolioRepository(mockEnv.Object, NullLogger<JsonPortfolioRepository>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task GetPortfolioAsync_NoFile_ReturnsEmptyPortfolio()
    {
        var portfolio = await _repo.GetPortfolioAsync("user-123");

        Assert.Equal("user-123", portfolio.UserId);
        Assert.Empty(portfolio.EquityHoldings);
        Assert.Empty(portfolio.MutualFundHoldings);
        Assert.Empty(portfolio.CashHoldings);
    }

    [Fact]
    public async Task SaveAndGet_RoundTripsPortfolio()
    {
        var portfolio = new UserPortfolio
        {
            UserId = "user-456",
            EquityHoldings = new List<EquityHolding>
            {
                new() { Symbol = "RELIANCE", CompanyName = "Reliance", Quantity = 10, AverageBuyPrice = 2500 }
            },
            CashHoldings = new List<CashHolding>
            {
                new() { BankName = "SBI", Balance = 50000 }
            }
        };

        await _repo.SavePortfolioAsync(portfolio);
        var loaded = await _repo.GetPortfolioAsync("user-456");

        Assert.Single(loaded.EquityHoldings);
        Assert.Equal("RELIANCE", loaded.EquityHoldings[0].Symbol);
        Assert.Equal(10, loaded.EquityHoldings[0].Quantity);
        Assert.Single(loaded.CashHoldings);
        Assert.Equal("SBI", loaded.CashHoldings[0].BankName);
        Assert.Equal(50000m, loaded.CashHoldings[0].Balance);
    }

    [Fact]
    public async Task SavePortfolio_UpdatesLastUpdated()
    {
        var portfolio = new UserPortfolio { UserId = "user-789" };
        var before = DateTime.UtcNow;

        await _repo.SavePortfolioAsync(portfolio);
        var loaded = await _repo.GetPortfolioAsync("user-789");

        Assert.True(loaded.LastUpdated >= before);
    }

    [Fact]
    public async Task SavePortfolio_CreatesJsonFile()
    {
        var portfolio = new UserPortfolio { UserId = "file-check" };
        await _repo.SavePortfolioAsync(portfolio);

        var filePath = Path.Combine(_tempDir, "Data", "portfolios", "file-check.json");
        Assert.True(File.Exists(filePath));

        var json = await File.ReadAllTextAsync(filePath);
        Assert.Contains("file-check", json);
    }

    [Fact]
    public async Task SavePortfolio_MutualFundHoldings_Persisted()
    {
        var portfolio = new UserPortfolio
        {
            UserId = "mf-user",
            MutualFundHoldings = new List<MutualFundHolding>
            {
                new() { SchemeCode = "119551", SchemeName = "Axis Bluechip", Units = 100, AverageNav = 45 }
            }
        };

        await _repo.SavePortfolioAsync(portfolio);
        var loaded = await _repo.GetPortfolioAsync("mf-user");

        Assert.Single(loaded.MutualFundHoldings);
        Assert.Equal("119551", loaded.MutualFundHoldings[0].SchemeCode);
        Assert.Equal(100m, loaded.MutualFundHoldings[0].Units);
    }

    [Fact]
    public async Task ReplaceByAccount_Equity_ReplacesOnlySameAccount()
    {
        var portfolio = new UserPortfolio
        {
            UserId = "acct-test",
            EquityHoldings = new List<EquityHolding>
            {
                new() { Symbol = "RELIANCE", AccountTag = "Zerodha", Quantity = 10, AverageBuyPrice = 2500 },
                new() { Symbol = "TCS", AccountTag = "Zerodha", Quantity = 5, AverageBuyPrice = 3200 },
                new() { Symbol = "INFY", AccountTag = "Groww", Quantity = 20, AverageBuyPrice = 1500 }
            }
        };
        await _repo.SavePortfolioAsync(portfolio);

        // Simulate replace-on-confirm for Zerodha account
        var loaded = await _repo.GetPortfolioAsync("acct-test");
        loaded.EquityHoldings.RemoveAll(h => h.AccountTag.Equals("Zerodha", StringComparison.OrdinalIgnoreCase));
        loaded.EquityHoldings.Add(new EquityHolding { Symbol = "HDFC", AccountTag = "Zerodha", Quantity = 15, AverageBuyPrice = 1600 });
        await _repo.SavePortfolioAsync(loaded);

        var result = await _repo.GetPortfolioAsync("acct-test");
        Assert.Equal(2, result.EquityHoldings.Count);
        Assert.Single(result.EquityHoldings.Where(h => h.AccountTag == "Zerodha"));
        Assert.Equal("HDFC", result.EquityHoldings.First(h => h.AccountTag == "Zerodha").Symbol);
        Assert.Single(result.EquityHoldings.Where(h => h.AccountTag == "Groww"));
        Assert.Equal("INFY", result.EquityHoldings.First(h => h.AccountTag == "Groww").Symbol);
    }

    [Fact]
    public async Task ReplaceByAccount_MutualFund_ReplacesOnlySameAccount()
    {
        var portfolio = new UserPortfolio
        {
            UserId = "acct-mf-test",
            MutualFundHoldings = new List<MutualFundHolding>
            {
                new() { SchemeName = "Axis Bluechip", AccountTag = "Groww", Units = 100, AverageNav = 45 },
                new() { SchemeName = "HDFC Mid-Cap", AccountTag = "Kuvera", Units = 50, AverageNav = 30 }
            }
        };
        await _repo.SavePortfolioAsync(portfolio);

        var loaded = await _repo.GetPortfolioAsync("acct-mf-test");
        loaded.MutualFundHoldings.RemoveAll(h => h.AccountTag.Equals("Groww", StringComparison.OrdinalIgnoreCase));
        loaded.MutualFundHoldings.Add(new MutualFundHolding { SchemeName = "SBI Small Cap", AccountTag = "Groww", Units = 200, AverageNav = 80 });
        await _repo.SavePortfolioAsync(loaded);

        var result = await _repo.GetPortfolioAsync("acct-mf-test");
        Assert.Equal(2, result.MutualFundHoldings.Count);
        Assert.Equal("SBI Small Cap", result.MutualFundHoldings.First(h => h.AccountTag == "Groww").SchemeName);
        Assert.Equal("HDFC Mid-Cap", result.MutualFundHoldings.First(h => h.AccountTag == "Kuvera").SchemeName);
    }

    [Fact]
    public async Task DeleteByAccount_RemovesOnlyTargetAccount()
    {
        var portfolio = new UserPortfolio
        {
            UserId = "del-acct-test",
            EquityHoldings = new List<EquityHolding>
            {
                new() { Symbol = "RELIANCE", AccountTag = "Zerodha", Quantity = 10, AverageBuyPrice = 2500 },
                new() { Symbol = "TCS", AccountTag = "Angel", Quantity = 5, AverageBuyPrice = 3200 }
            }
        };
        await _repo.SavePortfolioAsync(portfolio);

        var loaded = await _repo.GetPortfolioAsync("del-acct-test");
        loaded.EquityHoldings.RemoveAll(h => h.AccountTag.Equals("Zerodha", StringComparison.OrdinalIgnoreCase));
        await _repo.SavePortfolioAsync(loaded);

        var result = await _repo.GetPortfolioAsync("del-acct-test");
        Assert.Single(result.EquityHoldings);
        Assert.Equal("Angel", result.EquityHoldings[0].AccountTag);
    }
}
