namespace FinanceTracker.Web.Models;

public class CashHolding
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BankName { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Savings";
    public decimal Balance { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
