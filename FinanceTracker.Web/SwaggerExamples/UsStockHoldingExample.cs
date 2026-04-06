namespace FinanceTracker.Web.SwaggerExamples;

using FinanceTracker.Web.Models;
using Swashbuckle.AspNetCore.Filters;

/// <summary>Example for a single <see cref="UsStockHolding"/>.</summary>
public class UsStockHoldingExample : IExamplesProvider<UsStockHolding>
{
    public UsStockHolding GetExamples() => new()
    {
        Id = "e5f6a7b8-c9d0-1234-efab-345678901234",
        Symbol = "AAPL",
        CompanyName = "Apple Inc.",
        Quantity = 5m,
        AverageBuyPriceUsd = 165.00m,
        CurrentPriceUsd = 178.50m,
        ExchangeRateUsdInr = 83.50m,
        LastPriceUpdate = new DateTime(2025, 4, 5, 20, 0, 0, DateTimeKind.Utc),
        AddedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
