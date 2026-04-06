namespace FinanceTracker.Web.Services;

using System.Text.Json;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;

public class TagService : ITagService
{
    private readonly string _dataDir;
    private readonly ILogger<TagService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, List<string>> _cache = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TagService(IWebHostEnvironment env, ILogger<TagService> logger)
    {
        _dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(_dataDir);
        _logger = logger;
    }

    private string FilePath(string userId) => Path.Combine(_dataDir, $"tags_{userId}.json");

    public async Task<List<string>> GetTagsAsync(string userId)
    {
        await _lock.WaitAsync();
        try
        {
            return await LoadAsync(userId);
        }
        finally { _lock.Release(); }
    }

    public async Task AddTagsAsync(string userId, IEnumerable<string> tags)
    {
        var newTags = tags
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToList();

        if (!newTags.Any()) return;

        await _lock.WaitAsync();
        try
        {
            var existing = await LoadAsync(userId);
            var added = newTags.Except(existing, StringComparer.OrdinalIgnoreCase).ToList();
            if (!added.Any()) return;

            existing.AddRange(added);
            existing.Sort(StringComparer.OrdinalIgnoreCase);
            await SaveAsync(userId, existing);
            _logger.LogDebug("Added {Count} new tags for user {UserId}: {Tags}", added.Count, userId, string.Join(", ", added));
        }
        finally { _lock.Release(); }
    }

    public async Task DeleteTagAsync(string userId, string tag)
    {
        await _lock.WaitAsync();
        try
        {
            var existing = await LoadAsync(userId);
            var removed = existing.RemoveAll(t => t.Equals(tag.Trim(), StringComparison.OrdinalIgnoreCase));
            if (removed > 0) await SaveAsync(userId, existing);
        }
        finally { _lock.Release(); }
    }

    public async Task<List<string>> SearchTagsAsync(string userId, string query)
    {
        var all = await GetTagsAsync(userId);
        if (string.IsNullOrWhiteSpace(query)) return all;

        var q = query.Trim().ToLowerInvariant();
        return all.Where(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task SeedTagsIfEmptyAsync(string userId, IEnumerable<string> tags)
    {
        // Quick check without lock — if file exists the seed has already run (or user manages tags manually)
        if (File.Exists(FilePath(userId))) return;

        var seedTags = tags
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();

        if (!seedTags.Any()) return;

        await _lock.WaitAsync();
        try
        {
            // Re-check inside the lock to guard against a race between two concurrent requests
            if (File.Exists(FilePath(userId))) return;

            await SaveAsync(userId, seedTags);
            _logger.LogInformation("Seeded {Count} tags from existing expense data for user {UserId}", seedTags.Count, userId);
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
            var store = JsonSerializer.Deserialize<TagStore>(json, _jsonOptions);
            _cache[userId] = store?.Tags ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load tag store for user {UserId}", userId);
            _cache[userId] = new List<string>();
        }
        return _cache[userId];
    }

    private async Task SaveAsync(string userId, List<string> tags)
    {
        var store = new TagStore
        {
            UserId = userId,
            Tags = tags,
            LastModified = DateTime.UtcNow
        };
        await File.WriteAllTextAsync(FilePath(userId), JsonSerializer.Serialize(store, _jsonOptions));
        _cache[userId] = tags;
    }
}
