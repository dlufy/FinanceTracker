namespace FinanceTracker.Web.Models.ViewModels;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public class EquityUploadViewModel
{
    [Required(ErrorMessage = "Account/Broker name is required")]
    [Display(Name = "Account / Broker")]
    public string AccountTag { get; set; } = string.Empty;

    [Display(Name = "Holdings File (CSV or XLSX)")]
    public IFormFile? CsvFile { get; set; }

    public List<EquityHolding> Holdings { get; set; } = new();
    public List<EquityHolding> PreviewHoldings { get; set; } = new();
    public List<string> ExistingAccounts { get; set; } = new();
    public bool ShowPreview { get; set; }
    public string? PreviewFileId { get; set; }
    public string? PreviewAccountTag { get; set; }
    public string? UploadMessage { get; set; }
}
