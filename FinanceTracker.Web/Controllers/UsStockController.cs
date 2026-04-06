namespace FinanceTracker.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Models.ViewModels;
using FinanceTracker.Web.Services.Interfaces;

[Authorize]
public class UsStockController : Controller
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<UsStockController> _logger;

    public UsStockController(
        IPortfolioRepository portfolioRepository,
        IMarketDataService marketDataService,
        ILogger<UsStockController> logger)
    {
        _portfolioRepository = portfolioRepository;
        _marketDataService = marketDataService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task<UsStockViewModel> BuildViewModelAsync(UserPortfolio portfolio)
    {
        var rate = await _marketDataService.GetUsdToInrRateAsync();
        return new UsStockViewModel
        {
            Holdings = portfolio.UsStockHoldings,
            CurrentUsdInrRate = rate
        };
    }

    /// <summary>Renders the US Stocks portfolio management page.</summary>
    public async Task<IActionResult> Index()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        return View(await BuildViewModelAsync(portfolio));
    }

    /// <summary>
    /// Adds a new US stock holding, fetching the current USD price and USD/INR exchange rate automatically.
    /// </summary>
    /// <remarks>The <c>companyName</c> field is optional; the symbol is used as a fallback.</remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(UsStockViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var portfolio2 = await _portfolioRepository.GetPortfolioAsync(GetUserId());
            var vm2 = await BuildViewModelAsync(portfolio2);
            vm2.Symbol = model.Symbol;
            vm2.CompanyName = model.CompanyName;
            vm2.Quantity = model.Quantity;
            vm2.AverageBuyPriceUsd = model.AverageBuyPriceUsd;
            return View("Index", vm2);
        }

        var symbol = model.Symbol.Trim().ToUpper();

        // Fetch live price and rate
        var priceUsd = await _marketDataService.GetUsStockPriceUsdAsync(symbol);
        var rateUsdInr = await _marketDataService.GetUsdToInrRateAsync();

        var holding = new UsStockHolding
        {
            Symbol = symbol,
            CompanyName = string.IsNullOrWhiteSpace(model.CompanyName) ? symbol : model.CompanyName.Trim(),
            Quantity = model.Quantity,
            AverageBuyPriceUsd = model.AverageBuyPriceUsd,
            CurrentPriceUsd = priceUsd,
            ExchangeRateUsdInr = rateUsdInr > 0 ? rateUsdInr : 84m, // fallback rate
            LastPriceUpdate = DateTime.UtcNow
        };

        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.UsStockHoldings.Add(holding);
        await _portfolioRepository.SavePortfolioAsync(portfolio);

        _logger.LogInformation("User {UserId} added US stock {Symbol}: {Qty} shares @ ${Price}",
            GetUserId(), symbol, model.Quantity, model.AverageBuyPriceUsd);

        TempData["SuccessMessage"] = $"{symbol} added! Price: ${priceUsd:N2} (₹{holding.CurrentPriceInr:N2}) at ₹{rateUsdInr:N2}/USD.";
        return RedirectToAction("Index");
    }

    /// <summary>Deletes a US stock holding by its ID.</summary>
    /// <param name="id">The unique identifier of the holding to delete.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        var removed = portfolio.UsStockHoldings.RemoveAll(h => h.Id == id);
        await _portfolioRepository.SavePortfolioAsync(portfolio);
        if (removed > 0)
            TempData["SuccessMessage"] = "Holding removed.";
        return RedirectToAction("Index");
    }

    /// <summary>Clears all US stock holdings from the portfolio.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.UsStockHoldings.Clear();
        await _portfolioRepository.SavePortfolioAsync(portfolio);
        TempData["SuccessMessage"] = "All US stock holdings cleared.";
        return RedirectToAction("Index");
    }

    /// <summary>Refreshes live USD prices and USD/INR exchange rate for all US stock holdings.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshPrices()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        var rateUsdInr = await _marketDataService.GetUsdToInrRateAsync();

        foreach (var holding in portfolio.UsStockHoldings)
        {
            var price = await _marketDataService.GetUsStockPriceUsdAsync(holding.Symbol);
            if (price > 0)
            {
                holding.CurrentPriceUsd = price;
                holding.LastPriceUpdate = DateTime.UtcNow;
            }
            if (rateUsdInr > 0)
                holding.ExchangeRateUsdInr = rateUsdInr;
        }

        await _portfolioRepository.SavePortfolioAsync(portfolio);
        _logger.LogInformation("Refreshed US stock prices for user {UserId}, rate: {Rate}", GetUserId(), rateUsdInr);
        TempData["SuccessMessage"] = $"Prices refreshed! USD/INR: ₹{rateUsdInr:N2}";
        return RedirectToAction("Index");
    }
}
