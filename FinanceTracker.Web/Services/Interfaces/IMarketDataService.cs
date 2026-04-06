namespace FinanceTracker.Web.Services.Interfaces;

using FinanceTracker.Web.Models;

public interface IMarketDataService
{
    Task<decimal> GetStockPriceAsync(string symbol, string exchange = "NSE");
    Task<decimal> GetMutualFundNavAsync(string schemeCode);
    Task<Dictionary<string, decimal>> GetStockPricesBatchAsync(IEnumerable<string> symbols, string exchange = "NSE");
    Task<decimal> GetUsStockPriceUsdAsync(string symbol);
    Task<decimal> GetUsdToInrRateAsync();
}
