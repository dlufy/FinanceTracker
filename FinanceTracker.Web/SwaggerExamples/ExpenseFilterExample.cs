namespace FinanceTracker.Web.SwaggerExamples;

using FinanceTracker.Web.Models;
using Swashbuckle.AspNetCore.Filters;

/// <summary>Example for <see cref="ExpenseFilter"/> query parameters.</summary>
public class ExpenseFilterExample : IExamplesProvider<ExpenseFilter>
{
    public ExpenseFilter GetExamples() => new()
    {
        DateFrom = new DateTime(2025, 4, 1),
        DateTo = new DateTime(2025, 4, 30),
        Category = "Groceries",
        Tags = new List<string> { "essentials" },
        Page = 1,
        PageSize = 20
    };
}
