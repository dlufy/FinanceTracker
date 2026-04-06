namespace FinanceTracker.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Models.ViewModels;
using FinanceTracker.Web.Services.Interfaces;

[Authorize]
public class DashboardController : Controller
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IPortfolioRepository portfolioRepository, IMarketDataService marketDataService, ILogger<DashboardController> logger)
    {
        _portfolioRepository = portfolioRepository;
        _marketDataService = marketDataService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        await CaptureMonthlySnapshotIfNeeded(portfolio);

        var equityInvested = portfolio.EquityHoldings.Sum(h => h.InvestedValue);
        var equityCurrentValue = portfolio.EquityHoldings.Sum(h => h.CurrentValue);
        var mfInvested = portfolio.MutualFundHoldings.Sum(h => h.InvestedValue);
        var mfCurrentValue = portfolio.MutualFundHoldings.Sum(h => h.CurrentValue);
        var cashValue = portfolio.CashHoldings.Sum(h => h.Balance);
        var usStockInvested = portfolio.UsStockHoldings.Sum(h => h.InvestedValueInr);
        var usStockCurrentValue = portfolio.UsStockHoldings.Sum(h => h.CurrentValueInr);
        var usStockPL = usStockCurrentValue - usStockInvested;

        var totalInvested = equityInvested + mfInvested + cashValue + usStockInvested;
        var totalCurrentValue = equityCurrentValue + mfCurrentValue + cashValue + usStockCurrentValue;
        var totalPL = totalCurrentValue - totalInvested;

        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var expensesThisMonth = portfolio.Expenses
            .Where(e => e.Date.ToString("yyyy-MM") == currentMonth)
            .Sum(e => e.Amount);

        var viewModel = new DashboardViewModel
        {
            DisplayName = User.FindFirst("DisplayName")?.Value ?? User.Identity?.Name ?? "",
            TotalInvested = totalInvested,
            TotalCurrentValue = totalCurrentValue,
            TotalProfitLoss = totalPL,
            TotalProfitLossPercent = totalInvested == 0 ? 0 : (totalPL / totalInvested) * 100,
            EquityValue = equityCurrentValue,
            MutualFundValue = mfCurrentValue,
            CashValue = cashValue,
            UsStockValue = usStockCurrentValue,
            EquityPercent = totalCurrentValue == 0 ? 0 : (equityCurrentValue / totalCurrentValue) * 100,
            MutualFundPercent = totalCurrentValue == 0 ? 0 : (mfCurrentValue / totalCurrentValue) * 100,
            CashPercent = totalCurrentValue == 0 ? 0 : (cashValue / totalCurrentValue) * 100,
            UsStockPercent = totalCurrentValue == 0 ? 0 : (usStockCurrentValue / totalCurrentValue) * 100,
            EquityInvested = equityInvested,
            EquityProfitLoss = equityCurrentValue - equityInvested,
            MutualFundInvested = mfInvested,
            MutualFundProfitLoss = mfCurrentValue - mfInvested,
            UsStockInvested = usStockInvested,
            UsStockProfitLoss = usStockPL,
            TotalExpenses = portfolio.Expenses.Sum(e => e.Amount),
            ExpensesThisMonth = expensesThisMonth,
            TopEquities = portfolio.EquityHoldings.OrderByDescending(h => h.CurrentValue).Take(5).ToList(),
            TopMutualFunds = portfolio.MutualFundHoldings.OrderByDescending(h => h.CurrentValue).Take(5).ToList(),
            TopUsStocks = portfolio.UsStockHoldings.OrderByDescending(h => h.CurrentValueInr).Take(5).ToList(),
            ChartLabels = portfolio.MonthlySnapshots.Select(s => s.Month).ToList(),
            ChartInvestedValues = portfolio.MonthlySnapshots.Select(s => s.TotalInvested).ToList(),
            ChartCurrentValues = portfolio.MonthlySnapshots.Select(s => s.TotalCurrentValue).ToList()
        };

        _logger.LogDebug("Dashboard loaded for user {UserId}: totalValue={Total}", GetUserId(), totalCurrentValue);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshPrices()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        _logger.LogInformation("Refreshing prices for user {UserId}: {EquityCount} equities, {MFCount} mutual funds, {UsCount} US stocks",
            GetUserId(), portfolio.EquityHoldings.Count, portfolio.MutualFundHoldings.Count, portfolio.UsStockHoldings.Count);

        // Refresh equity prices
        foreach (var holding in portfolio.EquityHoldings)
        {
            var price = await _marketDataService.GetStockPriceAsync(holding.Symbol, holding.Exchange);
            if (price > 0)
            {
                holding.CurrentPrice = price;
                holding.LastPriceUpdate = DateTime.UtcNow;
            }
        }

        // Refresh MF NAVs
        foreach (var holding in portfolio.MutualFundHoldings)
        {
            var nav = await _marketDataService.GetMutualFundNavAsync(holding.SchemeCode);
            if (nav > 0)
            {
                holding.CurrentNav = nav;
                holding.LastNavUpdate = DateTime.UtcNow;
            }
        }

        // Refresh US stock prices + rate
        var usdInrRate = await _marketDataService.GetUsdToInrRateAsync();
        foreach (var holding in portfolio.UsStockHoldings)
        {
            var price = await _marketDataService.GetUsStockPriceUsdAsync(holding.Symbol);
            if (price > 0)
            {
                holding.CurrentPriceUsd = price;
                holding.LastPriceUpdate = DateTime.UtcNow;
            }
            if (usdInrRate > 0)
                holding.ExchangeRateUsdInr = usdInrRate;
        }

        await _portfolioRepository.SavePortfolioAsync(portfolio);
        _logger.LogInformation("Prices refreshed successfully for user {UserId}", GetUserId());
        TempData["SuccessMessage"] = "Prices refreshed successfully!";
        return RedirectToAction("Index");
    }

    private async Task CaptureMonthlySnapshotIfNeeded(UserPortfolio portfolio)
    {
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var existingSnapshot = portfolio.MonthlySnapshots.FirstOrDefault(s => s.Month == currentMonth);

        if (existingSnapshot != null) return;

        var equityValue = portfolio.EquityHoldings.Sum(h => h.CurrentValue);
        var mfValue = portfolio.MutualFundHoldings.Sum(h => h.CurrentValue);
        var cashValue = portfolio.CashHoldings.Sum(h => h.Balance);

        var snapshot = new MonthlySnapshot
        {
            Month = currentMonth,
            TotalInvested = portfolio.EquityHoldings.Sum(h => h.InvestedValue)
                          + portfolio.MutualFundHoldings.Sum(h => h.InvestedValue)
                          + cashValue,
            TotalCurrentValue = equityValue + mfValue + cashValue,
            EquityValue = equityValue,
            MutualFundValue = mfValue,
            CashValue = cashValue,
            SnapshotDate = DateTime.UtcNow
        };

        portfolio.MonthlySnapshots.Add(snapshot);
        await _portfolioRepository.SavePortfolioAsync(portfolio);
    }
}
