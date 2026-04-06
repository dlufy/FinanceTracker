namespace FinanceTracker.Web.SwaggerExamples;

using FinanceTracker.Web.Models;
using Swashbuckle.AspNetCore.Filters;

/// <summary>Example for a single <see cref="EquityHolding"/>.</summary>
public class EquityHoldingExample : IExamplesProvider<EquityHolding>
{
    public EquityHolding GetExamples() => new()
    {
        Id = "c3d4e5f6-a7b8-9012-cdef-123456789012",
        AccountTag = "Zerodha",
        Symbol = "RELIANCE",
        Isin = "INE002A01018",
        Exchange = "NSE",
        CompanyName = "Reliance Industries Ltd",
        Quantity = 10,
        AverageBuyPrice = 2400.00m,
        CurrentPrice = 2750.00m,
        LastPriceUpdate = new DateTime(2025, 4, 5, 10, 0, 0, DateTimeKind.Utc),
        AddedAt = new DateTime(2025, 1, 15, 8, 0, 0, DateTimeKind.Utc)
    };
}
