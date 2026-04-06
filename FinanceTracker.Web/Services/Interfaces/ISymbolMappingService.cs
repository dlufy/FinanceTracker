namespace FinanceTracker.Web.Services.Interfaces;

public interface ISymbolMappingService
{
    /// <summary>Returns cached (symbol, exchange) for a company name, or null if unknown.</summary>
    Task<(string symbol, string exchange)?> GetEquityMappingAsync(string userId, string companyName);

    /// <summary>Returns cached scheme code for a scheme name, or null if unknown.</summary>
    Task<string?> GetMfMappingAsync(string userId, string schemeName);

    /// <summary>Persists a confirmed company name → symbol mapping for future uploads.</summary>
    Task SaveEquityMappingAsync(string userId, string companyName, string symbol, string exchange);

    /// <summary>Persists a confirmed scheme name → scheme code mapping for future uploads.</summary>
    Task SaveMfMappingAsync(string userId, string schemeName, string schemeCode);
}
