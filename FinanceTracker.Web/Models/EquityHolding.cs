namespace FinanceTracker.Web.Models;

public class EquityHolding
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AccountTag { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Isin { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NSE";
    public string CompanyName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal InvestedValue => Quantity * AverageBuyPrice;
    public decimal CurrentValue => Quantity * CurrentPrice;
    public decimal ProfitLoss => CurrentValue - InvestedValue;
    public decimal ProfitLossPercent => InvestedValue == 0 ? 0 : (ProfitLoss / InvestedValue) * 100;
    public DateTime LastPriceUpdate { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
