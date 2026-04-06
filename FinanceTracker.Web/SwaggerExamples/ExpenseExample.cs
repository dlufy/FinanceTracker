namespace FinanceTracker.Web.SwaggerExamples;

using FinanceTracker.Web.Models;
using Swashbuckle.AspNetCore.Filters;

/// <summary>Example for a single <see cref="Expense"/>.</summary>
public class ExpenseExample : IExamplesProvider<Expense>
{
    public Expense GetExamples() => new()
    {
        Id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        Date = new DateTime(2025, 4, 1),
        Amount = 850.00m,
        Category = "Groceries",
        Description = "Weekly grocery run at BigBasket",
        Tags = new List<string> { "essentials", "food" },
        Source = "Manual",
        AddedAt = new DateTime(2025, 4, 1, 18, 30, 0, DateTimeKind.Utc)
    };
}
