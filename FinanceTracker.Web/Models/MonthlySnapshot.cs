namespace FinanceTracker.Web.Models;

public class MonthlySnapshot
{
    public string Month { get; set; } = string.Empty; // "2026-03"
    public decimal TotalInvested { get; set; }
    public decimal TotalCurrentValue { get; set; }
    public decimal EquityValue { get; set; }
    public decimal MutualFundValue { get; set; }
    public decimal CashValue { get; set; }
    public decimal UsStockValue { get; set; }
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
}
