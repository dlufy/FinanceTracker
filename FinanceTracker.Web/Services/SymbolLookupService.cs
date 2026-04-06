namespace FinanceTracker.Web.Services;

using System.Text.Json;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;

public class SymbolLookupService : ISymbolLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SymbolLookupService> _logger;

    // Yahoo Finance search — returns top equity results for Indian market
    private const string YahooSearchUrl =
        "https://query1.finance.yahoo.com/v1/finance/search?q={0}&lang=en-IN&region=IN&quotesCount=8&newsCount=0&listsCount=0&enableFuzzyQuery=false&enableNavLinks=false";

    // MFAPI full-text search
    private const string MfApiSearchUrl = "https://api.mfapi.in/mf/search?q={0}";

    public SymbolLookupService(HttpClient httpClient, ILogger<SymbolLookupService> logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FinanceTracker/1.0");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        _logger = logger;
    }

    public async Task<List<EquitySymbolCandidate>> SearchEquityAsync(string query, CancellationToken ct = default)
    {
        var candidates = new List<EquitySymbolCandidate>();
        if (string.IsNullOrWhiteSpace(query)) return candidates;

        try
        {
            var url = string.Format(YahooSearchUrl, Uri.EscapeDataString(query.Trim()));
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yahoo Finance search returned {StatusCode} for query: {Query}", response.StatusCode, query);
                return candidates;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("quotes", out var quotes))
                return candidates;

            foreach (var quote in quotes.EnumerateArray())
            {
                if (!quote.TryGetProperty("quoteType", out var typeEl)) continue;
                var quoteType = typeEl.GetString() ?? string.Empty;
                if (!quoteType.Equals("EQUITY", StringComparison.OrdinalIgnoreCase)) continue;

                if (!quote.TryGetProperty("symbol", out var symEl)) continue;
                var yahooSymbol = symEl.GetString() ?? string.Empty;

                // Only NSE (.NS) and BSE (.BO) symbols
                string exchange;
                string bareSymbol;
                if (yahooSymbol.EndsWith(".NS", StringComparison.OrdinalIgnoreCase))
                {
                    exchange = "NSE";
                    bareSymbol = yahooSymbol[..^3];
                }
                else if (yahooSymbol.EndsWith(".BO", StringComparison.OrdinalIgnoreCase))
                {
                    exchange = "BSE";
                    bareSymbol = yahooSymbol[..^3];
                }
                else
                {
                    continue; // skip non-Indian exchanges
                }

                var name = quote.TryGetProperty("longname", out var ln) ? ln.GetString() ?? string.Empty
                    : quote.TryGetProperty("shortname", out var sn) ? sn.GetString() ?? string.Empty
                    : bareSymbol;

                candidates.Add(new EquitySymbolCandidate
                {
                    Symbol = bareSymbol,
                    Name = name,
                    Exchange = exchange,
                    YahooSymbol = yahooSymbol
                });
            }

            _logger.LogDebug("Yahoo symbol search for '{Query}' returned {Count} candidates", query, candidates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Yahoo Finance for equity: {Query}", query);
        }

        return candidates;
    }

    public async Task<List<MfSchemeCandidate>> SearchMutualFundAsync(string query, CancellationToken ct = default)
    {
        var candidates = new List<MfSchemeCandidate>();
        if (string.IsNullOrWhiteSpace(query)) return candidates;

        try
        {
            var url = string.Format(MfApiSearchUrl, Uri.EscapeDataString(query.Trim()));
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MFAPI search returned {StatusCode} for query: {Query}", response.StatusCode, query);
                return candidates;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var schemeCode = item.TryGetProperty("schemeCode", out var sc)
                    ? sc.GetInt32().ToString()
                    : string.Empty;
                var schemeName = item.TryGetProperty("schemeName", out var sn)
                    ? sn.GetString() ?? string.Empty
                    : string.Empty;

                if (!string.IsNullOrEmpty(schemeCode) && !string.IsNullOrEmpty(schemeName))
                    candidates.Add(new MfSchemeCandidate { SchemeCode = schemeCode, SchemeName = schemeName });
            }

            // Return only first 5 most relevant results
            candidates = candidates.Take(5).ToList();
            _logger.LogDebug("MFAPI search for '{Query}' returned {Count} candidates", query, candidates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching MFAPI for mutual fund: {Query}", query);
        }

        return candidates;
    }
}
