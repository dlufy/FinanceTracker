namespace FinanceTracker.Web.SwaggerExamples;

using FinanceTracker.Web.Models;
using Swashbuckle.AspNetCore.Filters;

/// <summary>Example for a paginated <see cref="PagedResult{Expense}"/> response.</summary>
public class PagedExpenseResultExample : IExamplesProvider<PagedResult<Expense>>
{
    public PagedResult<Expense> GetExamples() => new()
    {
        Items = new List<Expense>
        {
            new()
            {
                Id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                Date = new DateTime(2025, 4, 1),
                Amount = 850.00m,
                Category = "Groceries",
                Description = "Weekly grocery run at BigBasket",
                Tags = new List<string> { "essentials", "food" },
                Source = "Manual",
                AddedAt = new DateTime(2025, 4, 1, 18, 30, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = "b2c3d4e5-f6a7-8901-bcde-f12345678901",
                Date = new DateTime(2025, 4, 3),
                Amount = 1200.00m,
                Category = "Utilities",
                Description = "Electricity bill",
                Tags = new List<string> { "bills" },
                Source = "CSV",
                AddedAt = new DateTime(2025, 4, 3, 9, 0, 0, DateTimeKind.Utc)
            }
        },
        TotalCount = 42,
        TotalAmount = 18500.50m,
        CategoryTotals = new Dictionary<string, decimal>
        {
            { "Groceries", 5200.00m },
            { "Utilities", 3100.00m },
            { "Transport", 2800.50m }
        },
        Page = 1,
        PageSize = 20
    };
}
