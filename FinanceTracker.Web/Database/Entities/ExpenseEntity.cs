namespace FinanceTracker.Web.Database.Entities;

public class ExpenseEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Stored as PostgreSQL text[] array.</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    public string Source { get; set; } = "Manual";
    public DateTime AddedAt { get; set; }

    // Navigation
    public UserEntity User { get; set; } = null!;
}
