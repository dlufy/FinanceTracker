namespace FinanceTracker.Web.Models.ViewModels;

public class DashboardViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    // Summary totals
    public decimal TotalInvested { get; set; }
    public decimal TotalCurrentValue { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public decimal TotalProfitLossPercent { get; set; }

    // Allocation breakdown
    public decimal EquityValue { get; set; }
    public decimal MutualFundValue { get; set; }
    public decimal CashValue { get; set; }
    public decimal UsStockValue { get; set; }
    public decimal EquityPercent { get; set; }
    public decimal MutualFundPercent { get; set; }
    public decimal CashPercent { get; set; }
    public decimal UsStockPercent { get; set; }

    // Equity P&L
    public decimal EquityInvested { get; set; }
    public decimal EquityProfitLoss { get; set; }

    // MF P&L
    public decimal MutualFundInvested { get; set; }
    public decimal MutualFundProfitLoss { get; set; }

    // US Stock P&L
    public decimal UsStockInvested { get; set; }
    public decimal UsStockProfitLoss { get; set; }

    // Expenses
    public decimal TotalExpenses { get; set; }
    public decimal ExpensesThisMonth { get; set; }

    // Top holdings
    public List<EquityHolding> TopEquities { get; set; } = new();
    public List<MutualFundHolding> TopMutualFunds { get; set; } = new();
    public List<UsStockHolding> TopUsStocks { get; set; } = new();

    // Monthly chart data
    public List<string> ChartLabels { get; set; } = new();
    public List<decimal> ChartInvestedValues { get; set; } = new();
    public List<decimal> ChartCurrentValues { get; set; } = new();
}
