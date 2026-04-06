namespace FinanceTracker.Web.Models;

public class Expense
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Source { get; set; } = "Manual"; // "Manual" or "CSV"
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
