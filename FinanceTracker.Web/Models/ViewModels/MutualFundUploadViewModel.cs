namespace FinanceTracker.Web.Models.ViewModels;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public class MutualFundUploadViewModel
{
    [Required(ErrorMessage = "Account/Broker name is required")]
    [Display(Name = "Account / Broker")]
    public string AccountTag { get; set; } = string.Empty;

    [Display(Name = "Holdings File (CSV or XLSX)")]
    public IFormFile? CsvFile { get; set; }

    public List<MutualFundHolding> Holdings { get; set; } = new();
    public List<MutualFundHolding> PreviewHoldings { get; set; } = new();
    public List<string> ExistingAccounts { get; set; } = new();
    public bool ShowPreview { get; set; }
    public string? PreviewFileId { get; set; }
    public string? PreviewAccountTag { get; set; }
    public string? UploadMessage { get; set; }

    /// <summary>Holdings with no SchemeCode — shown in the mapping UI.</summary>
    public List<MfResolutionItem> UnresolvedFunds { get; set; } = new();
    public bool HasUnresolvedFunds => UnresolvedFunds.Any();
}
