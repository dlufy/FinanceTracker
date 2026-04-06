namespace FinanceTracker.Web.Services;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;

public class MarketDataService : IMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MarketDataService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public MarketDataService(HttpClient httpClient, IMemoryCache cache, ILogger<MarketDataService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<decimal> GetStockPriceAsync(string symbol, string exchange = "NSE")
    {
        var cacheKey = $"stock_{symbol}_{exchange}";
        if (_cache.TryGetValue(cacheKey, out decimal cachedPrice))
            return cachedPrice;

        try
        {
            var suffix = exchange.ToUpper() == "BSE" ? ".BO" : ".NS";
            var yahooSymbol = $"{symbol}{suffix}";
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{yahooSymbol}?range=1d&interval=1d";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var price = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0]
                .GetProperty("meta")
                .GetProperty("regularMarketPrice")
                .GetDecimal();
        _logger.LogInformation("Fetched price for {Symbol} on {Exchange}: {Price}", symbol, exchange, price);

            _cache.Set(cacheKey, price, CacheDuration);
            return price;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch price for {Symbol} on {Exchange}", symbol, exchange);
            return 0;
        }
    }

    public async Task<decimal> GetMutualFundNavAsync(string schemeCode)
    {
        var cacheKey = $"mf_{schemeCode}";
        if (_cache.TryGetValue(cacheKey, out decimal cachedNav))
            return cachedNav;

        try
        {
            var url = $"https://api.mfapi.in/mf/{schemeCode}/latest";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var navString = doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("nav")
                .GetString();

            _logger.LogInformation("Fetched NAV for scheme {SchemeCode}: {NAV}", schemeCode, navString);

            if (decimal.TryParse(navString, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var nav))
            {
                _cache.Set(cacheKey, nav, CacheDuration);
                return nav;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch NAV for scheme {SchemeCode}", schemeCode);
        }
        return 0;
    }

    public async Task<Dictionary<string, decimal>> GetStockPricesBatchAsync(
        IEnumerable<string> symbols, string exchange = "NSE")
    {
        var result = new Dictionary<string, decimal>();
        var tasks = symbols.Select(async symbol =>
        {
            var price = await GetStockPriceAsync(symbol, exchange);
            return (symbol, price);
        });

        foreach (var task in await Task.WhenAll(tasks))
        {
            result[task.symbol] = task.price;
        }
        return result;
    }

    public async Task<decimal> GetUsStockPriceUsdAsync(string symbol)
    {
        var cacheKey = $"usstk_{symbol.ToUpper()}";
        if (_cache.TryGetValue(cacheKey, out decimal cachedPrice))
        {
            _logger.LogDebug("Cache hit for US stock {Symbol}: ${Price}", symbol, cachedPrice);
            return cachedPrice;
        }

        try
        {
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol.ToUpper()}?range=1d&interval=1d";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var price = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0]
                .GetProperty("meta")
                .GetProperty("regularMarketPrice")
                .GetDecimal();

            _cache.Set(cacheKey, price, CacheDuration);
            _logger.LogInformation("Fetched US stock {Symbol}: ${Price} USD", symbol, price);
            return price;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch US stock price for {Symbol}", symbol);
            return 0;
        }
    }

    public async Task<decimal> GetUsdToInrRateAsync()
    {
        const string cacheKey = "usd_inr_rate";
        if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
        {
            _logger.LogDebug("Cache hit for USD/INR rate: {Rate}", cachedRate);
            return cachedRate;
        }

        try
        {
            var url = "https://query1.finance.yahoo.com/v8/finance/chart/USDINR=X?range=1d&interval=1d";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var rate = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0]
                .GetProperty("meta")
                .GetProperty("regularMarketPrice")
                .GetDecimal();

            _cache.Set(cacheKey, rate, CacheDuration);
            _logger.LogInformation("Fetched USD/INR rate: {Rate}", rate);
            return rate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch USD/INR exchange rate");
            return 0;
        }
    }
}
