namespace FinanceTracker.Web.Controllers;

using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Models.ViewModels;
using FinanceTracker.Web.Services.Interfaces;

[Authorize]
public class EquityController : Controller
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IFileParserService _fileParserService;
    private readonly IMarketDataService _marketDataService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EquityController> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public EquityController(
        IPortfolioRepository portfolioRepository,
        IFileParserService fileParserService,
        IMarketDataService marketDataService,
        IWebHostEnvironment env,
        ILogger<EquityController> logger)
    {
        _portfolioRepository = portfolioRepository;
        _fileParserService = fileParserService;
        _marketDataService = marketDataService;
        _env = env;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private string GetPreviewDir()
    {
        var dir = Path.Combine(_env.ContentRootPath, "Data", "previews");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private EquityUploadViewModel BuildViewModel(UserPortfolio portfolio)
    {
        return new EquityUploadViewModel
        {
            Holdings = portfolio.EquityHoldings,
            ExistingAccounts = portfolio.EquityHoldings
                .Select(h => h.AccountTag)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList()
        };
    }

    public async Task<IActionResult> Index()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        return View(BuildViewModel(portfolio));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(EquityUploadViewModel model)
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());

        if (string.IsNullOrWhiteSpace(model.AccountTag))
        {
            var vm = BuildViewModel(portfolio);
            vm.UploadMessage = "Please enter an account/broker name (e.g., Zerodha, Groww).";
            return View("Index", vm);
        }

        if (model.CsvFile == null || model.CsvFile.Length == 0)
        {
            var vm = BuildViewModel(portfolio);
            vm.AccountTag = model.AccountTag;
            vm.UploadMessage = "Please select a valid CSV or XLSX file.";
            return View("Index", vm);
        }

        try
        {
            using var stream = model.CsvFile.OpenReadStream();
            var parsed = _fileParserService.ParseEquityFile(stream, model.CsvFile.FileName);
            _logger.LogInformation("Parsed {Count} equity holdings from {FileName} for account {AccountTag}",
                parsed.Count, model.CsvFile.FileName, model.AccountTag);

            if (!parsed.Any())
            {
                var vm = BuildViewModel(portfolio);
                vm.AccountTag = model.AccountTag;
                vm.UploadMessage = "No holdings found in the uploaded file. Please check the format.";
                return View("Index", vm);
            }

            foreach (var h in parsed)
                h.AccountTag = model.AccountTag.Trim();

            // Save preview data to server-side file
            var userId = GetUserId();
            var previewId = $"{userId}_equity_{Guid.NewGuid():N}";
            var previewPath = Path.Combine(GetPreviewDir(), $"{previewId}.json");
            var previewPayload = new { AccountTag = model.AccountTag.Trim(), Holdings = parsed };
            await System.IO.File.WriteAllTextAsync(previewPath, JsonSerializer.Serialize(previewPayload, _jsonOptions));

            TempData["PreviewFileId"] = previewId;

            var existingCount = portfolio.EquityHoldings.Count(h =>
                h.AccountTag.Equals(model.AccountTag.Trim(), StringComparison.OrdinalIgnoreCase));

            var vm2 = BuildViewModel(portfolio);
            vm2.AccountTag = model.AccountTag;
            vm2.PreviewHoldings = parsed;
            vm2.PreviewAccountTag = model.AccountTag.Trim();
            vm2.PreviewFileId = previewId;
            vm2.ShowPreview = true;
            vm2.UploadMessage = existingCount > 0
                ? $"Preview: {parsed.Count} holdings from \"{model.AccountTag.Trim()}\". This will replace {existingCount} existing holdings for this account."
                : $"Preview: {parsed.Count} holdings from \"{model.AccountTag.Trim()}\". Review and confirm below.";
            return View("Index", vm2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing equity file {FileName} for user {UserId}", model.CsvFile?.FileName, GetUserId());
            var vm = BuildViewModel(portfolio);
            vm.AccountTag = model.AccountTag;
            vm.UploadMessage = $"Error parsing file: {ex.Message}";
            return View("Index", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPreview(string id)
    {
        if (string.IsNullOrEmpty(id))
            return RedirectToAction("Index");

        var previewPath = Path.Combine(GetPreviewDir(), $"{id}.json");
        if (!System.IO.File.Exists(previewPath))
        {
            TempData["ErrorMessage"] = "Preview file not found. Please upload again.";
            return RedirectToAction("Index");
        }

        var json = await System.IO.File.ReadAllTextAsync(previewPath);
        using var doc = JsonDocument.Parse(json);
        var holdingsElement = doc.RootElement.GetProperty("holdings");
        var holdings = JsonSerializer.Deserialize<List<EquityHolding>>(holdingsElement.GetRawText(), _jsonOptions) ?? new();
        var accountTag = doc.RootElement.GetProperty("accountTag").GetString() ?? "Unknown";

        var csv = new StringBuilder();
        csv.AppendLine("Account,Symbol,CompanyName,ISIN,Exchange,Quantity,AverageBuyPrice,BuyValue,CurrentPrice,CurrentValue,ProfitLoss");
        foreach (var h in holdings)
        {
            csv.AppendLine(string.Join(",",
                Escape(accountTag), Escape(h.Symbol), Escape(h.CompanyName), Escape(h.Isin), Escape(h.Exchange),
                h.Quantity.ToString(CultureInfo.InvariantCulture),
                h.AverageBuyPrice.ToString("F2", CultureInfo.InvariantCulture),
                h.InvestedValue.ToString("F2", CultureInfo.InvariantCulture),
                h.CurrentPrice.ToString("F2", CultureInfo.InvariantCulture),
                h.CurrentValue.ToString("F2", CultureInfo.InvariantCulture),
                h.ProfitLoss.ToString("F2", CultureInfo.InvariantCulture)));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"preview_equity_{accountTag}.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmUpload()
    {
        var previewId = TempData["PreviewFileId"] as string;

        if (string.IsNullOrEmpty(previewId))
        {
            TempData["ErrorMessage"] = "No preview data found. Please upload the file again.";
            return RedirectToAction("Index");
        }

        var previewPath = Path.Combine(GetPreviewDir(), $"{previewId}.json");
        if (!System.IO.File.Exists(previewPath))
        {
            TempData["ErrorMessage"] = "Preview file expired. Please upload the file again.";
            return RedirectToAction("Index");
        }

        var json = await System.IO.File.ReadAllTextAsync(previewPath);
        using var doc = JsonDocument.Parse(json);
        var holdingsElement = doc.RootElement.GetProperty("holdings");
        var accountTag = doc.RootElement.GetProperty("accountTag").GetString() ?? string.Empty;
        var holdings = JsonSerializer.Deserialize<List<EquityHolding>>(holdingsElement.GetRawText(), _jsonOptions) ?? new();

        // Fetch current prices for all holdings with a valid symbol
        foreach (var holding in holdings)
        {
            if (holding.CurrentPrice == 0 && !string.IsNullOrWhiteSpace(holding.Symbol))
            {
                var price = await _marketDataService.GetStockPriceAsync(holding.Symbol, holding.Exchange);
                if (price > 0)
                {
                    holding.CurrentPrice = price;
                    holding.LastPriceUpdate = DateTime.UtcNow;
                }
            }
        }

        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.EquityHoldings.RemoveAll(h =>
            h.AccountTag.Equals(accountTag, StringComparison.OrdinalIgnoreCase));
        portfolio.EquityHoldings.AddRange(holdings);
        await _portfolioRepository.SavePortfolioAsync(portfolio);

        // Cleanup preview file
        try { System.IO.File.Delete(previewPath); } catch { }

        _logger.LogInformation("Confirmed upload: {Count} equity holdings saved for account {AccountTag}, user {UserId}",
            holdings.Count, accountTag, GetUserId());

        TempData["SuccessMessage"] = $"{holdings.Count} equity holdings saved for \"{accountTag}\"!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.EquityHoldings.RemoveAll(h => h.Id == id);
        await _portfolioRepository.SavePortfolioAsync(portfolio);
        TempData["SuccessMessage"] = "Holding removed.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string accountTag)
    {
        if (string.IsNullOrWhiteSpace(accountTag))
            return RedirectToAction("Index");

        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        var removed = portfolio.EquityHoldings.RemoveAll(h =>
            h.AccountTag.Equals(accountTag.Trim(), StringComparison.OrdinalIgnoreCase));
        await _portfolioRepository.SavePortfolioAsync(portfolio);

        _logger.LogInformation("Deleted {Count} equity holdings for account {AccountTag}, user {UserId}",
            removed, accountTag, GetUserId());

        TempData["SuccessMessage"] = $"Removed {removed} holdings from \"{accountTag}\".";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.EquityHoldings.Clear();
        await _portfolioRepository.SavePortfolioAsync(portfolio);
        TempData["SuccessMessage"] = "All equity holdings cleared.";
        return RedirectToAction("Index");
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
