namespace FinanceTracker.Web.Models;

public class UserPortfolio
{
    public string UserId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<EquityHolding> EquityHoldings { get; set; } = new();
    public List<MutualFundHolding> MutualFundHoldings { get; set; } = new();
    public List<CashHolding> CashHoldings { get; set; } = new();
    public List<UsStockHolding> UsStockHoldings { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
    public List<MonthlySnapshot> MonthlySnapshots { get; set; } = new();
}
