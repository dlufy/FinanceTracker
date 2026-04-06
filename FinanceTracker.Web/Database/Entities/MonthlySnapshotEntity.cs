namespace FinanceTracker.Web.Database.Entities;

public class MonthlySnapshotEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Format: "2026-03"</summary>
    public string Month { get; set; } = string.Empty;

    public decimal TotalInvested { get; set; }
    public decimal TotalCurrentValue { get; set; }
    public decimal EquityValue { get; set; }
    public decimal MutualFundValue { get; set; }
    public decimal CashValue { get; set; }
    public decimal UsStockValue { get; set; }
    public DateTime SnapshotDate { get; set; }

    // Navigation
    public UserEntity User { get; set; } = null!;
}
