namespace FinanceTracker.Web.Models;

/// <summary>Candidate ticker symbol returned by Yahoo Finance search for an Indian equity.</summary>
public class EquitySymbolCandidate
{
    public string Symbol { get; set; } = string.Empty;    // e.g. "RELIANCE"
    public string Name { get; set; } = string.Empty;      // e.g. "Reliance Industries Limited"
    public string Exchange { get; set; } = string.Empty;  // "NSE" or "BSE"
    public string YahooSymbol { get; set; } = string.Empty; // e.g. "RELIANCE.NS"
}

/// <summary>Candidate scheme returned by MFAPI search.</summary>
public class MfSchemeCandidate
{
    public string SchemeCode { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
}

/// <summary>One equity holding whose symbol couldn't be resolved automatically.</summary>
public class EquityResolutionItem
{
    public string HoldingId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Isin { get; set; } = string.Empty;
    public List<EquitySymbolCandidate> Candidates { get; set; } = new();
}

/// <summary>One MF holding whose scheme code couldn't be resolved automatically.</summary>
public class MfResolutionItem
{
    public string HoldingId { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public List<MfSchemeCandidate> Candidates { get; set; } = new();
}
