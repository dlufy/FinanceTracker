namespace FinanceTracker.Web.Controllers;

using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Models.ViewModels;
using FinanceTracker.Web.Services;
using FinanceTracker.Web.Services.Interfaces;

[Authorize]
public class ExpenseController : Controller
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IFileParserService _fileParserService;
    private readonly IExpenseQueryService _expenseQueryService;
    private readonly ITagService _tagService;
    private readonly ICategoryService _categoryService;
    private readonly ChannelWriter<ExpenseImportJob> _importChannel;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExpenseController> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExpenseController(
        IPortfolioRepository portfolioRepository,
        IFileParserService fileParserService,
        IExpenseQueryService expenseQueryService,
        ITagService tagService,
        ICategoryService categoryService,
        ChannelWriter<ExpenseImportJob> importChannel,
        IWebHostEnvironment env,
        ILogger<ExpenseController> logger)
    {
        _portfolioRepository = portfolioRepository;
        _fileParserService = fileParserService;
        _expenseQueryService = expenseQueryService;
        _tagService = tagService;
        _categoryService = categoryService;
        _importChannel = importChannel;
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

    private async Task<ExpenseViewModel> BuildViewModelAsync()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        var userId = GetUserId();

        // One-time migration: seed services from data already present in existing expenses
        var expenseTags = portfolio.Expenses.SelectMany(e => e.Tags).Where(t => !string.IsNullOrEmpty(t));
        var expenseCategories = portfolio.Expenses.Select(e => e.Category).Where(c => !string.IsNullOrEmpty(c));
        await Task.WhenAll(
            _tagService.SeedTagsIfEmptyAsync(userId, expenseTags),
            _categoryService.SeedCategoriesIfEmptyAsync(userId, expenseCategories));

        var allTags = await _tagService.GetTagsAsync(userId);

        return new ExpenseViewModel
        {
            // Categories are now loaded via AJAX (/Expense/Categories) to stay fresh after async imports
            ExistingCategories = await _categoryService.GetCategoriesAsync(userId),
            AllTags = allTags
        };
    }

    public async Task<IActionResult> Index()
    {
        return View(await BuildViewModelAsync());
    }

    /// <summary>AJAX endpoint — returns filtered, paginated expenses as JSON.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ExpenseFilter filter)
    {
        var result = await _expenseQueryService.GetFilteredAsync(GetUserId(), filter);
        return Json(result, _jsonOptions);
    }

    /// <summary>AJAX — returns current category list as JSON (refreshed without page reload).</summary>
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _categoryService.GetCategoriesAsync(GetUserId());
        return Json(categories);
    }

    /// <summary>AJAX autocomplete for categories.</summary>
    [HttpGet]
    public async Task<IActionResult> CategorySuggestions(string q = "")
    {
        var categories = await _categoryService.SearchCategoriesAsync(GetUserId(), q);
        return Json(categories);
    }

    /// <summary>AJAX autocomplete for tags.</summary>
    [HttpGet]
    public async Task<IActionResult> TagSuggestions(string q = "")
    {
        var tags = await _tagService.SearchTagsAsync(GetUserId(), q);
        return Json(tags);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(ExpenseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var vm2 = await BuildViewModelAsync();
            vm2.Date = model.Date;
            vm2.Amount = model.Amount;
            vm2.Category = model.Category;
            vm2.Description = model.Description;
            return View("Index", vm2);
        }

        var tags = ParseTags(model.TagsInput);

        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        var expense = new Expense
        {
            Date = model.Date,
            Amount = model.Amount,
            Category = model.Category.Trim(),
            Description = model.Description?.Trim() ?? string.Empty,
            Tags = tags,
            Source = "Manual"
        };
        portfolio.Expenses.Add(expense);
        await _portfolioRepository.SavePortfolioAsync(portfolio);

        // Register new tags and category (fire-and-forget style, background task handles it)
        _ = _tagService.AddTagsAsync(GetUserId(), tags);
        _ = _categoryService.AddCategoriesAsync(GetUserId(), new[] { expense.Category });

        _logger.LogInformation("User {UserId} added expense: {Category} ₹{Amount} on {Date} tags=[{Tags}]",
            GetUserId(), expense.Category, expense.Amount, expense.Date, string.Join(",", tags));

        TempData["SuccessMessage"] = "Expense added.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(ExpenseViewModel model)
    {
        if (model.CsvFile == null || model.CsvFile.Length == 0)
        {
            var vm = await BuildViewModelAsync();
            vm.UploadMessage = "Please select a valid CSV or XLSX file.";
            return View("Index", vm);
        }

        try
        {
            using var stream = model.CsvFile.OpenReadStream();
            var parsed = _fileParserService.ParseExpenseFile(stream, model.CsvFile.FileName);

            if (!parsed.Any())
            {
                var vm = await BuildViewModelAsync();
                vm.UploadMessage = "No expenses found in the uploaded file. Please check the format.";
                return View("Index", vm);
            }

            foreach (var e in parsed) e.Source = "CSV";

            var previewId = $"{GetUserId()}_expense_{Guid.NewGuid():N}";
            var previewPath = Path.Combine(GetPreviewDir(), $"{previewId}.json");
            await System.IO.File.WriteAllTextAsync(previewPath, JsonSerializer.Serialize(parsed, _jsonOptions));
            TempData["PreviewFileId"] = previewId;

            var vm2 = await BuildViewModelAsync();
            vm2.PreviewExpenses = parsed;
            vm2.PreviewFileId = previewId;
            vm2.ShowPreview = true;
            vm2.UploadMessage = $"Preview: {parsed.Count} expenses parsed. Review and confirm below.";
            return View("Index", vm2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing expense file {FileName}", model.CsvFile?.FileName);
            var vm = await BuildViewModelAsync();
            vm.UploadMessage = $"Error parsing file: {ex.Message}";
            return View("Index", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPreview(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var previewPath = Path.Combine(GetPreviewDir(), $"{id}.json");
        if (!System.IO.File.Exists(previewPath))
        {
            TempData["ErrorMessage"] = "Preview file not found. Please upload again.";
            return RedirectToAction("Index");
        }

        var json = await System.IO.File.ReadAllTextAsync(previewPath);
        var expenses = JsonSerializer.Deserialize<List<Expense>>(json, _jsonOptions) ?? new();

        var csv = new StringBuilder();
        csv.AppendLine("Date,Amount,Category,Description,Tags");
        foreach (var e in expenses)
            csv.AppendLine(string.Join(",",
                e.Date.ToString("yyyy-MM-dd"),
                e.Amount.ToString("F2", CultureInfo.InvariantCulture),
                Escape(e.Category),
                Escape(e.Description),
                Escape(string.Join(";", e.Tags))));

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "preview_expenses.csv");
    }

    /// <summary>Enqueues the previewed expenses for async background processing. Returns immediately.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmUpload()
    {
        var previewId = TempData["PreviewFileId"] as string;
        if (string.IsNullOrEmpty(previewId))
        {
            TempData["ErrorMessage"] = "No preview data found. Please upload again.";
            return RedirectToAction("Index");
        }

        var previewPath = Path.Combine(GetPreviewDir(), $"{previewId}.json");
        if (!System.IO.File.Exists(previewPath))
        {
            TempData["ErrorMessage"] = "Preview file expired. Please upload again.";
            return RedirectToAction("Index");
        }

        var json = await System.IO.File.ReadAllTextAsync(previewPath);
        var expenses = JsonSerializer.Deserialize<List<Expense>>(json, _jsonOptions) ?? new();

        var job = new ExpenseImportJob(GetUserId(), previewId, expenses);
        await _importChannel.WriteAsync(job);

        _logger.LogInformation("Queued expense import: {Count} expenses for user {UserId}", expenses.Count, GetUserId());
        TempData["ImportQueued"] = $"{expenses.Count} expenses are being processed and will appear shortly.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.Expenses.RemoveAll(e => e.Id == id);
        await _portfolioRepository.SavePortfolioAsync(portfolio);
        TempData["SuccessMessage"] = "Expense removed.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll()
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(GetUserId());
        portfolio.Expenses.Clear();
        await _portfolioRepository.SavePortfolioAsync(portfolio);
        TempData["SuccessMessage"] = "All expenses cleared.";
        return RedirectToAction("Index");
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static List<string> ParseTags(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? new List<string>()
            : raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                 .Select(t => t.Trim())
                 .Where(t => !string.IsNullOrEmpty(t))
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .ToList();
}
