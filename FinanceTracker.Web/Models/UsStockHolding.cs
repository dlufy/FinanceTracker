namespace FinanceTracker.Web.Models;

public class UsStockHolding
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageBuyPriceUsd { get; set; }
    public decimal CurrentPriceUsd { get; set; }
    public decimal ExchangeRateUsdInr { get; set; }
    public DateTime LastPriceUpdate { get; set; } = DateTime.UtcNow;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Computed — INR values
    public decimal AverageBuyPriceInr => AverageBuyPriceUsd * ExchangeRateUsdInr;
    public decimal CurrentPriceInr => CurrentPriceUsd * ExchangeRateUsdInr;
    public decimal InvestedValueUsd => Quantity * AverageBuyPriceUsd;
    public decimal CurrentValueUsd => Quantity * CurrentPriceUsd;
    public decimal InvestedValueInr => Quantity * AverageBuyPriceInr;
    public decimal CurrentValueInr => Quantity * CurrentPriceInr;
    public decimal ProfitLossInr => CurrentValueInr - InvestedValueInr;
    public decimal ProfitLossPercent => InvestedValueInr == 0 ? 0
        : (ProfitLossInr / InvestedValueInr) * 100;
}
