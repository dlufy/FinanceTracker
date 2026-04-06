namespace FinanceTracker.Web.Services.Interfaces;

using FinanceTracker.Web.Models;

public interface ISymbolLookupService
{
    /// <summary>Search Yahoo Finance for Indian equity tickers matching <paramref name="query"/>.</summary>
    Task<List<EquitySymbolCandidate>> SearchEquityAsync(string query, CancellationToken ct = default);

    /// <summary>Search MFAPI for mutual fund schemes matching <paramref name="query"/>.</summary>
    Task<List<MfSchemeCandidate>> SearchMutualFundAsync(string query, CancellationToken ct = default);
}
