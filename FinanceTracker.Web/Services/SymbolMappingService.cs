namespace FinanceTracker.Web.Services;

using System.Text.Json;
using FinanceTracker.Web.Services.Interfaces;

/// <summary>
/// Persists per-user symbol mappings in Data/symbol_mappings_{userId}.json.
/// Stores: company name → {symbol, exchange} for equities; scheme name → schemeCode for MFs.
/// </summary>
public class SymbolMappingService : ISymbolMappingService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SymbolMappingService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    // In-memory cache: userId → MappingFile
    private readonly Dictionary<string, MappingFile> _cache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SymbolMappingService(IWebHostEnvironment env, ILogger<SymbolMappingService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<(string symbol, string exchange)?> GetEquityMappingAsync(string userId, string companyName)
    {
        var file = await LoadAsync(userId);
        var key = Normalise(companyName);
        if (file.Equities.TryGetValue(key, out var entry))
            return (entry.Symbol, entry.Exchange);
        return null;
    }

    public async Task<string?> GetMfMappingAsync(string userId, string schemeName)
    {
        var file = await LoadAsync(userId);
        var key = Normalise(schemeName);
        file.MutualFunds.TryGetValue(key, out var code);
        return string.IsNullOrEmpty(code) ? null : code;
    }

    public async Task SaveEquityMappingAsync(string userId, string companyName, string symbol, string exchange)
    {
        var file = await LoadAsync(userId);
        file.Equities[Normalise(companyName)] = new EquityEntry { Symbol = symbol.ToUpperInvariant(), Exchange = exchange.ToUpperInvariant() };
        await SaveAsync(userId, file);
        _logger.LogInformation("Saved equity mapping: '{Name}' → {Symbol} ({Exchange}) for user {UserId}", companyName, symbol, exchange, userId);
    }

    public async Task SaveMfMappingAsync(string userId, string schemeName, string schemeCode)
    {
        var file = await LoadAsync(userId);
        file.MutualFunds[Normalise(schemeName)] = schemeCode;
        await SaveAsync(userId, file);
        _logger.LogInformation("Saved MF mapping: '{Name}' → {Code} for user {UserId}", schemeName, schemeCode, userId);
    }

    // ── Internals ─────────────────────────────────────────────

    private static string Normalise(string s) => s.Trim().ToLowerInvariant();

    private string GetFilePath(string userId)
    {
        var dir = Path.Combine(_env.ContentRootPath, "Data");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"symbol_mappings_{userId}.json");
    }

    private async Task<MappingFile> LoadAsync(string userId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(userId, out var cached))
                return cached;

            var path = GetFilePath(userId);
            if (!File.Exists(path))
            {
                var empty = new MappingFile();
                _cache[userId] = empty;
                return empty;
            }

            var json = await File.ReadAllTextAsync(path);
            var file = JsonSerializer.Deserialize<MappingFile>(json, _jsonOptions) ?? new MappingFile();
            _cache[userId] = file;
            return file;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load symbol mappings for user {UserId}, using empty", userId);
            return new MappingFile();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveAsync(string userId, MappingFile file)
    {
        await _lock.WaitAsync();
        try
        {
            _cache[userId] = file;
            var path = GetFilePath(userId);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(file, _jsonOptions));
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── File structure ────────────────────────────────────────

    private class MappingFile
    {
        public Dictionary<string, EquityEntry> Equities { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> MutualFunds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private class EquityEntry
    {
        public string Symbol { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
    }
}
