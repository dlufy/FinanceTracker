namespace FinanceTracker.Web.SwaggerExamples;

using FinanceTracker.Web.Models;
using Swashbuckle.AspNetCore.Filters;

/// <summary>Example for a single <see cref="MutualFundHolding"/>.</summary>
public class MutualFundHoldingExample : IExamplesProvider<MutualFundHolding>
{
    public MutualFundHolding GetExamples() => new()
    {
        Id = "d4e5f6a7-b8c9-0123-defa-234567890123",
        AccountTag = "Groww",
        SchemeCode = "120503",
        SchemeName = "Mirae Asset Large Cap Fund - Direct Plan - Growth",
        Amc = "Mirae Asset Mutual Fund",
        Category = "Equity - Large Cap",
        FolioNumber = "12345678",
        Units = 150.456m,
        AverageNav = 82.50m,
        CurrentNav = 96.20m,
        LastNavUpdate = new DateTime(2025, 4, 5, 0, 0, 0, DateTimeKind.Utc),
        AddedAt = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc)
    };
}
