namespace FinanceTracker.Web.Services;

using System.Text.Json;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;

public class CategoryService : ICategoryService
{
    private readonly string _dataDir;
    private readonly ILogger<CategoryService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, List<string>> _cache = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CategoryService(IWebHostEnvironment env, ILogger<CategoryService> logger)
    {
        _dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(_dataDir);
        _logger = logger;
    }

    private string FilePath(string userId) => Path.Combine(_dataDir, $"categories_{userId}.json");

    public async Task<List<string>> GetCategoriesAsync(string userId)
    {
        await _lock.WaitAsync();
        try { return await LoadAsync(userId); }
        finally { _lock.Release(); }
    }

    public async Task AddCategoriesAsync(string userId, IEnumerable<string> categories)
    {
        var newCats = categories
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!newCats.Any()) return;

        await _lock.WaitAsync();
        try
        {
            var existing = await LoadAsync(userId);
            var added = newCats.Except(existing, StringComparer.OrdinalIgnoreCase).ToList();
            if (!added.Any()) return;

            existing.AddRange(added);
            existing.Sort(StringComparer.OrdinalIgnoreCase);
            await SaveAsync(userId, existing);
            _logger.LogDebug("Added {Count} new categories for user {UserId}: {Categories}",
                added.Count, userId, string.Join(", ", added));
        }
        finally { _lock.Release(); }
    }

    public async Task DeleteCategoryAsync(string userId, string category)
    {
        await _lock.WaitAsync();
        try
        {
            var existing = await LoadAsync(userId);
            var removed = existing.RemoveAll(c => c.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
            if (removed > 0) await SaveAsync(userId, existing);
        }
        finally { _lock.Release(); }
    }

    public async Task<List<string>> SearchCategoriesAsync(string userId, string query)
    {
        var all = await GetCategoriesAsync(userId);
        if (string.IsNullOrWhiteSpace(query)) return all;

        var q = query.Trim();
        return all.Where(c => c.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task SeedCategoriesIfEmptyAsync(string userId, IEnumerable<string> categories)
    {
        if (File.Exists(FilePath(userId))) return;

        var seedCats = categories
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!seedCats.Any()) return;

        await _lock.WaitAsync();
        try
        {
            if (File.Exists(FilePath(userId))) return;
            await SaveAsync(userId, seedCats);
            _logger.LogInformation("Seeded {Count} categories from existing expense data for user {UserId}",
                seedCats.Count, userId);
        }
        finally { _lock.Release(); }
    }

    // Must be called while holding _lock
    private async Task<List<string>> LoadAsync(string userId)
    {
        if (_cache.TryGetValue(userId, out var cached)) return cached;

        var path = FilePath(userId);
        if (!File.Exists(path))
        {
            _cache[userId] = new List<string>();
            return _cache[userId];
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var store = JsonSerializer.Deserialize<CategoryStore>(json, _jsonOptions);
            _cache[userId] = store?.Categories ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load category store for user {UserId}", userId);
            _cache[userId] = new List<string>();
        }
        return _cache[userId];
    }

    private async Task SaveAsync(string userId, List<string> categories)
    {
        var store = new CategoryStore
        {
            UserId = userId,
            Categories = categories,
            LastModified = DateTime.UtcNow
        };
        await File.WriteAllTextAsync(FilePath(userId), JsonSerializer.Serialize(store, _jsonOptions));
        _cache[userId] = categories;
    }
}
