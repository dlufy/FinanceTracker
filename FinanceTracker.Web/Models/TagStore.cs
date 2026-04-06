namespace FinanceTracker.Web.Models;

public class TagStore
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
