namespace FinanceTracker.Web.SwaggerExamples;

using FinanceTracker.Web.Models;
using Swashbuckle.AspNetCore.Filters;

/// <summary>Example for a single <see cref="CashHolding"/>.</summary>
public class CashHoldingExample : IExamplesProvider<CashHolding>
{
    public CashHolding GetExamples() => new()
    {
        Id = "f6a7b8c9-d0e1-2345-fabc-456789012345",
        BankName = "HDFC Bank",
        AccountType = "Savings",
        Balance = 75000.00m,
        LastUpdated = new DateTime(2025, 4, 5, 0, 0, 0, DateTimeKind.Utc)
    };
}
