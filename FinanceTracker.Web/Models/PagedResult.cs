namespace FinanceTracker.Web.Models;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Sum of Amount per Category across all matching expenses (not just current page).</summary>
    public Dictionary<string, decimal> CategoryTotals { get; set; } = new();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
