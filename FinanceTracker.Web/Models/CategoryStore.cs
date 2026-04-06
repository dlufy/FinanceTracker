namespace FinanceTracker.Web.Models;

public class CategoryStore
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
