namespace FinanceTracker.Web.Models;

public class ExpenseFilter
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    /// <summary>Empty/null means no category filter (all categories).</summary>
    public string? Category { get; set; }

    /// <summary>Empty list means no tag filter (all tags).</summary>
    public List<string> Tags { get; set; } = new();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Returns effective DateFrom: defaults to the 1st day of the current month.</summary>
    public DateTime EffectiveDateFrom =>
        DateFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    /// <summary>Returns effective DateTo: defaults to today.</summary>
    public DateTime EffectiveDateTo =>
        DateTo?.Date ?? DateTime.Today;

    public bool HasCategoryFilter => !string.IsNullOrWhiteSpace(Category);
    public bool HasTagFilter => Tags.Any();
}
