namespace FinanceTracker.Web.Models;

public class MutualFundHolding
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AccountTag { get; set; } = string.Empty;
    public string SchemeCode { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string Amc { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FolioNumber { get; set; } = string.Empty;
    public decimal Units { get; set; }
    public decimal AverageNav { get; set; }
    public decimal CurrentNav { get; set; }
    public decimal InvestedValue => Units * AverageNav;
    public decimal CurrentValue => Units * CurrentNav;
    public decimal ProfitLoss => CurrentValue - InvestedValue;
    public decimal ProfitLossPercent => InvestedValue == 0 ? 0 : (ProfitLoss / InvestedValue) * 100;
    public DateTime LastNavUpdate { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
