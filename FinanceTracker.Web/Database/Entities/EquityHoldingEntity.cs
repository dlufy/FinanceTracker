namespace FinanceTracker.Web.Database.Entities;

public class EquityHoldingEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountTag { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Isin { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NSE";
    public string CompanyName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime LastPriceUpdate { get; set; }
    public DateTime AddedAt { get; set; }

    // Navigation
    public UserEntity User { get; set; } = null!;
}
