namespace FinanceTracker.Web.Database.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<EquityHoldingEntity> EquityHoldings { get; set; } = new List<EquityHoldingEntity>();
    public ICollection<MutualFundHoldingEntity> MutualFundHoldings { get; set; } = new List<MutualFundHoldingEntity>();
    public ICollection<CashHoldingEntity> CashHoldings { get; set; } = new List<CashHoldingEntity>();
    public ICollection<UsStockHoldingEntity> UsStockHoldings { get; set; } = new List<UsStockHoldingEntity>();
    public ICollection<ExpenseEntity> Expenses { get; set; } = new List<ExpenseEntity>();
    public ICollection<MonthlySnapshotEntity> MonthlySnapshots { get; set; } = new List<MonthlySnapshotEntity>();
}
