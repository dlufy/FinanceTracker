namespace FinanceTracker.Web.Services;

using System.Threading.Channels;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;

/// <summary>Job payload queued when a user confirms a CSV expense upload.</summary>
public record ExpenseImportJob(
    string UserId,
    string PreviewFileId,
    List<Expense> Expenses
);

/// <summary>
/// Background service that consumes <see cref="ExpenseImportJob"/> items from a channel
/// and persists them to the portfolio repository.
/// </summary>
public class ExpenseImportBackgroundService : BackgroundService
{
    private readonly ChannelReader<ExpenseImportJob> _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpenseImportBackgroundService> _logger;
    private readonly string _previewDir;

    public ExpenseImportBackgroundService(
        ChannelReader<ExpenseImportJob> reader,
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env,
        ILogger<ExpenseImportBackgroundService> logger)
    {
        _reader = reader;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _previewDir = Path.Combine(env.ContentRootPath, "Data", "previews");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpenseImportBackgroundService started");

        await foreach (var job in _reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process expense import job for user {UserId}, preview {PreviewId}",
                    job.UserId, job.PreviewFileId);
            }
        }

        _logger.LogInformation("ExpenseImportBackgroundService stopped");
    }

    private async Task ProcessJobAsync(ExpenseImportJob job, CancellationToken ct)
    {
        _logger.LogInformation("Processing expense import: {Count} expenses for user {UserId}",
            job.Expenses.Count, job.UserId);

        // Use a scope so we get a properly resolved IPortfolioRepository (handles both JSON + Postgres modes)
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();

        var portfolio = await repo.GetPortfolioAsync(job.UserId);
        portfolio.Expenses.AddRange(job.Expenses);
        await repo.SavePortfolioAsync(portfolio);

        // Register any new tags and categories found in the imported expenses
        var allTags = job.Expenses.SelectMany(e => e.Tags).Distinct(StringComparer.OrdinalIgnoreCase);
        var allCategories = job.Expenses.Select(e => e.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct(StringComparer.OrdinalIgnoreCase);
        await Task.WhenAll(
            tagService.AddTagsAsync(job.UserId, allTags),
            categoryService.AddCategoriesAsync(job.UserId, allCategories));

        // Delete the preview file
        var previewPath = Path.Combine(_previewDir, $"{job.PreviewFileId}.json");
        try { File.Delete(previewPath); } catch { /* already cleaned up */ }

        _logger.LogInformation("Expense import complete: {Count} expenses saved for user {UserId}",
            job.Expenses.Count, job.UserId);
    }
}
