namespace FinanceTracker.Web.Database.Entities;

public class CashHoldingEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Savings";
    public decimal Balance { get; set; }
    public DateTime LastUpdated { get; set; }

    // Navigation
    public UserEntity User { get; set; } = null!;
}
