namespace FinanceTracker.Web.Database.Entities;

public class UsStockHoldingEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageBuyPriceUsd { get; set; }
    public decimal CurrentPriceUsd { get; set; }
    public decimal ExchangeRateUsdInr { get; set; }
    public DateTime LastPriceUpdate { get; set; }
    public DateTime AddedAt { get; set; }

    // Navigation
    public UserEntity User { get; set; } = null!;
}
