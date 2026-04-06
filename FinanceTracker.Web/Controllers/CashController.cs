namespace FinanceTracker.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Models.ViewModels;
using FinanceTracker.Web.Services.Interfaces;

[Authorize]
public class CashController : Controller
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ILogger<CashController> _logger;

    public CashController(IPortfolioRepository portfolioRepository, ILogger<CashController> logger)
    {
        _portfolioRepository = portfolioRepository;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Renders the Cash holdings management page.</summary>
    public async Task<IActionResult> Index()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        var viewModel = new CashEntryViewModel
        {
            CashHoldings = portfolio.CashHoldings
        };
        return View(viewModel);
    }

    /// <summary>Adds a new cash/bank holding (savings, FD, current account, etc.).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(CashEntryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var portfolio2 = await _portfolioRepository.GetPortfolioAsync(GetUserId());
            model.CashHoldings = portfolio2.CashHoldings;
            return View("Index", model);
        }

        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        var cash = new CashHolding
        {
            BankName = model.BankName,
            AccountType = model.AccountType,
            Balance = model.Balance
        };

        portfolio.CashHoldings.Add(cash);
        await _portfolioRepository.SavePortfolioAsync(portfolio);

        _logger.LogInformation("User {UserId} added cash holding: {Bank} {AccountType} ₹{Balance}",
            GetUserId(), model.BankName, model.AccountType, model.Balance);

        TempData["SuccessMessage"] = "Cash holding added successfully!";
        return RedirectToAction("Index");
    }

    /// <summary>Deletes a cash holding by its ID.</summary>
    /// <param name="id">The unique identifier of the cash holding to delete.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.CashHoldings.RemoveAll(h => h.Id == id);
        await _portfolioRepository.SavePortfolioAsync(portfolio);
        TempData["SuccessMessage"] = "Cash holding removed.";
        return RedirectToAction("Index");
    }

    /// <summary>Renders the edit form pre-populated with an existing cash holding's details.</summary>
    /// <param name="id">The unique identifier of the cash holding to edit.</param>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        var cash = portfolio.CashHoldings.FirstOrDefault(h => h.Id == id);
        if (cash == null) return RedirectToAction("Index");

        var viewModel = new CashEntryViewModel
        {
            Id = cash.Id,
            BankName = cash.BankName,
            AccountType = cash.AccountType,
            Balance = cash.Balance,
            CashHoldings = portfolio.CashHoldings
        };
        return View("Index", viewModel);
    }

    /// <summary>Saves changes to an existing cash holding.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(CashEntryViewModel model)
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(model.Id))
        {
            var portfolio2 = await _portfolioRepository.GetPortfolioAsync(GetUserId());
            model.CashHoldings = portfolio2.CashHoldings;
            return View("Index", model);
        }

        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        var cash = portfolio.CashHoldings.FirstOrDefault(h => h.Id == model.Id);
        if (cash != null)
        {
            cash.BankName = model.BankName;
            cash.AccountType = model.AccountType;
            cash.Balance = model.Balance;
            cash.LastUpdated = DateTime.UtcNow;
            await _portfolioRepository.SavePortfolioAsync(portfolio);
            TempData["SuccessMessage"] = "Cash holding updated!";
        }

        return RedirectToAction("Index");
    }
}
