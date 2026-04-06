namespace FinanceTracker.Web.Models.ViewModels;

using System.ComponentModel.DataAnnotations;

public class UsStockViewModel
{
    [Required(ErrorMessage = "Stock symbol is required (e.g., MSFT, AAPL)")]
    [Display(Name = "Symbol")]
    public string Symbol { get; set; } = string.Empty;

    [Display(Name = "Company Name (optional)")]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    [Display(Name = "Quantity")]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Average buy price must be greater than 0")]
    [Display(Name = "Avg Buy Price (USD)")]
    public decimal AverageBuyPriceUsd { get; set; }

    public List<UsStockHolding> Holdings { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    // For display: current USD/INR rate
    public decimal CurrentUsdInrRate { get; set; }
}
