namespace FinanceTracker.Web.Controllers;

using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Models.ViewModels;
using FinanceTracker.Web.Services.Interfaces;

[Authorize]
public class MutualFundController : Controller
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IFileParserService _fileParserService;
    private readonly IMarketDataService _marketDataService;
    private readonly ISymbolLookupService _symbolLookup;
    private readonly ISymbolMappingService _symbolMapping;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MutualFundController> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public MutualFundController(
        IPortfolioRepository portfolioRepository,
        IFileParserService fileParserService,
        IMarketDataService marketDataService,
        ISymbolLookupService symbolLookup,
        ISymbolMappingService symbolMapping,
        IWebHostEnvironment env,
        ILogger<MutualFundController> logger)
    {
        _portfolioRepository = portfolioRepository;
        _fileParserService = fileParserService;
        _marketDataService = marketDataService;
        _symbolLookup = symbolLookup;
        _symbolMapping = symbolMapping;
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

    private MutualFundUploadViewModel BuildViewModel(UserPortfolio portfolio)
    {
        return new MutualFundUploadViewModel
        {
            Holdings = portfolio.MutualFundHoldings,
            ExistingAccounts = portfolio.MutualFundHoldings
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
    public async Task<IActionResult> Preview(MutualFundUploadViewModel model)
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());

        if (string.IsNullOrWhiteSpace(model.AccountTag))
        {
            var vm = BuildViewModel(portfolio);
            vm.UploadMessage = "Please enter an account/broker name (e.g., Groww, Kuvera).";
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
            var parsed = _fileParserService.ParseMutualFundFile(stream, model.CsvFile.FileName);
            _logger.LogInformation("Parsed {Count} MF holdings from {FileName} for account {AccountTag}",
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

            // ── Scheme code resolution ────────────────────────────────────
            var userId = GetUserId();
            var unresolvedItems = new List<MfResolutionItem>();

            foreach (var h in parsed)
            {
                if (!string.IsNullOrWhiteSpace(h.SchemeCode)) continue;

                // Check user's mapping cache first
                var cached = await _symbolMapping.GetMfMappingAsync(userId, h.SchemeName);
                if (cached != null)
                {
                    h.SchemeCode = cached;
                    _logger.LogDebug("Resolved MF '{Name}' from cache → {Code}", h.SchemeName, h.SchemeCode);
                    continue;
                }

                // Cache miss — call MFAPI search
                var candidates = await _symbolLookup.SearchMutualFundAsync(h.SchemeName);
                if (candidates.Count > 0)
                {
                    // Auto-apply best match
                    h.SchemeCode = candidates[0].SchemeCode;
                    _logger.LogInformation("Auto-resolved MF '{Name}' → {Code} from MFAPI", h.SchemeName, h.SchemeCode);
                }

                // Add to unresolved list if ambiguous or no results
                if (candidates.Count != 1)
                {
                    unresolvedItems.Add(new MfResolutionItem
                    {
                        HoldingId = h.Id,
                        SchemeName = h.SchemeName,
                        Candidates = candidates.Take(5).ToList()
                    });
                }
                else
                {
                    await _symbolMapping.SaveMfMappingAsync(userId, h.SchemeName, h.SchemeCode);
                }
            }

            // Save preview data to server-side file
            var previewId = $"{userId}_mf_{Guid.NewGuid():N}";
            var previewPath = Path.Combine(GetPreviewDir(), $"{previewId}.json");
            var previewPayload = new { AccountTag = model.AccountTag.Trim(), Holdings = parsed };
            await System.IO.File.WriteAllTextAsync(previewPath, JsonSerializer.Serialize(previewPayload, _jsonOptions));

            TempData["PreviewFileId"] = previewId;

            var existingCount = portfolio.MutualFundHoldings.Count(h =>
                h.AccountTag.Equals(model.AccountTag.Trim(), StringComparison.OrdinalIgnoreCase));

            var vm2 = BuildViewModel(portfolio);
            vm2.AccountTag = model.AccountTag;
            vm2.PreviewHoldings = parsed;
            vm2.PreviewAccountTag = model.AccountTag.Trim();
            vm2.PreviewFileId = previewId;
            vm2.ShowPreview = true;
            vm2.UnresolvedFunds = unresolvedItems;
            vm2.UploadMessage = existingCount > 0
                ? $"Preview: {parsed.Count} holdings from \"{model.AccountTag.Trim()}\". This will replace {existingCount} existing holdings for this account."
                : $"Preview: {parsed.Count} holdings from \"{model.AccountTag.Trim()}\". Review and confirm below.";
            return View("Index", vm2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing MF file {FileName} for user {UserId}", model.CsvFile?.FileName, GetUserId());
            var vm = BuildViewModel(portfolio);
            vm.AccountTag = model.AccountTag;
            vm.UploadMessage = $"Error parsing file: {ex.Message}";
            return View("Index", vm);
        }
    }

    /// <summary>
    /// AJAX endpoint: apply user-confirmed scheme code mappings to the server-side preview JSON and cache them.
    /// </summary>
    /// <param name="previewFileId">The server-side preview file ID returned during the Preview step.</param>
    /// <param name="mappingsJson">JSON array of <c>[{holdingId, schemeCode}]</c> mapping objects.</param>
    /// <returns>JSON object with <c>success: true</c>, or <c>success: false, error: "..."</c> on failure.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Produces("application/json")]
    [SwaggerOperation(Summary = "Apply scheme code mappings to MF preview (AJAX)", Tags = new[] { "Mutual Funds" })]
    [SwaggerResponse(200, "Success or failure result", typeof(object))]
    public async Task<IActionResult> UpdateSchemes([FromForm] string previewFileId, [FromForm] string mappingsJson)
    {
        try
        {
            var previewPath = Path.Combine(GetPreviewDir(), $"{previewFileId}.json");
            if (!System.IO.File.Exists(previewPath))
                return Json(new { success = false, error = "Preview file not found. Please upload again." });

            var json = await System.IO.File.ReadAllTextAsync(previewPath);
            using var doc = JsonDocument.Parse(json);
            var accountTag = doc.RootElement.GetProperty("accountTag").GetString() ?? string.Empty;
            var holdings = JsonSerializer.Deserialize<List<MutualFundHolding>>(
                doc.RootElement.GetProperty("holdings").GetRawText(), _jsonOptions) ?? new();

            var mappings = JsonSerializer.Deserialize<List<SchemeMappingInput>>(mappingsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var userId = GetUserId();
            foreach (var mapping in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.SchemeCode)) continue;
                var holding = holdings.FirstOrDefault(h => h.Id == mapping.HoldingId);
                if (holding == null) continue;

                holding.SchemeCode = mapping.SchemeCode;
                await _symbolMapping.SaveMfMappingAsync(userId, holding.SchemeName, holding.SchemeCode);
            }

            var updated = new { AccountTag = accountTag, Holdings = holdings };
            await System.IO.File.WriteAllTextAsync(previewPath, JsonSerializer.Serialize(updated, _jsonOptions));
            TempData["PreviewFileId"] = previewFileId;

            _logger.LogInformation("Applied {Count} scheme mappings to preview {PreviewId}", mappings.Count, previewFileId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying scheme mappings");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>Live scheme name search for autocomplete in the mapping UI.</summary>
    /// <param name="q">Search query — minimum 2 characters. Searches MFAPI by scheme name.</param>
    /// <returns>Array of <c>{schemeCode, schemeName}</c> objects (up to 10 results).</returns>
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation(Summary = "Search mutual fund schemes (AJAX)", Tags = new[] { "Mutual Funds" })]
    [SwaggerResponse(200, "Matching schemes", typeof(object[]))]
    public async Task<IActionResult> SearchScheme(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(Array.Empty<object>());

        var candidates = await _symbolLookup.SearchMutualFundAsync(q);
        return Json(candidates.Select(c => new { c.SchemeCode, c.SchemeName }));
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
        var holdings = JsonSerializer.Deserialize<List<MutualFundHolding>>(holdingsElement.GetRawText(), _jsonOptions) ?? new();
        var accountTag = doc.RootElement.GetProperty("accountTag").GetString() ?? "Unknown";

        var csv = new StringBuilder();
        csv.AppendLine("Account,SchemeName,SchemeCode,AMC,Category,FolioNumber,Units,AverageNAV,InvestedValue,CurrentNAV,CurrentValue,ProfitLoss");
        foreach (var h in holdings)
        {
            csv.AppendLine(string.Join(",",
                Escape(accountTag), Escape(h.SchemeName), Escape(h.SchemeCode), Escape(h.Amc), Escape(h.Category),
                Escape(h.FolioNumber),
                h.Units.ToString("F4", CultureInfo.InvariantCulture),
                h.AverageNav.ToString("F4", CultureInfo.InvariantCulture),
                h.InvestedValue.ToString("F2", CultureInfo.InvariantCulture),
                h.CurrentNav.ToString("F4", CultureInfo.InvariantCulture),
                h.CurrentValue.ToString("F2", CultureInfo.InvariantCulture),
                h.ProfitLoss.ToString("F2", CultureInfo.InvariantCulture)));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"preview_mutualfund_{accountTag}.csv");
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
        var holdings = JsonSerializer.Deserialize<List<MutualFundHolding>>(holdingsElement.GetRawText(), _jsonOptions) ?? new();

        foreach (var holding in holdings)
        {
            if (!string.IsNullOrEmpty(holding.SchemeCode))
            {
                var nav = await _marketDataService.GetMutualFundNavAsync(holding.SchemeCode);
                if (nav > 0)
                {
                    holding.CurrentNav = nav;
                    holding.LastNavUpdate = DateTime.UtcNow;
                }
            }
        }

        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.MutualFundHoldings.RemoveAll(h =>
            h.AccountTag.Equals(accountTag, StringComparison.OrdinalIgnoreCase));
        portfolio.MutualFundHoldings.AddRange(holdings);
        await _portfolioRepository.SavePortfolioAsync(portfolio);

        // Cleanup preview file
        try { System.IO.File.Delete(previewPath); } catch { }

        _logger.LogInformation("Confirmed upload: {Count} MF holdings saved for account {AccountTag}, user {UserId}",
            holdings.Count, accountTag, GetUserId());

        TempData["SuccessMessage"] = $"{holdings.Count} mutual fund holdings saved for \"{accountTag}\"!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.MutualFundHoldings.RemoveAll(h => h.Id == id);
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
        var removed = portfolio.MutualFundHoldings.RemoveAll(h =>
            h.AccountTag.Equals(accountTag.Trim(), StringComparison.OrdinalIgnoreCase));
        await _portfolioRepository.SavePortfolioAsync(portfolio);

        _logger.LogInformation("Deleted {Count} MF holdings for account {AccountTag}, user {UserId}",
            removed, accountTag, GetUserId());

        TempData["SuccessMessage"] = $"Removed {removed} holdings from \"{accountTag}\".";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.MutualFundHoldings.Clear();
        await _portfolioRepository.SavePortfolioAsync(portfolio);
        TempData["SuccessMessage"] = "All mutual fund holdings cleared.";
        return RedirectToAction("Index");
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private record SchemeMappingInput(string HoldingId, string SchemeCode);
}

