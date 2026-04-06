namespace FinanceTracker.Web.Models.ViewModels;

using System.ComponentModel.DataAnnotations;

public class CashEntryViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Bank name is required")]
    [Display(Name = "Bank Name")]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account type is required")]
    [Display(Name = "Account Type")]
    public string AccountType { get; set; } = "Savings";

    [Required(ErrorMessage = "Balance is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Balance must be a positive number")]
    [Display(Name = "Balance (₹)")]
    public decimal Balance { get; set; }

    public List<CashHolding> CashHoldings { get; set; } = new();
}
