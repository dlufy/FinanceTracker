namespace FinanceTracker.Web.Models.ViewModels;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public class ExpenseViewModel
{
    [Required(ErrorMessage = "Date is required")]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Comma-separated tags string from the form input.</summary>
    public string TagsInput { get; set; } = string.Empty;

    // Upload
    public IFormFile? CsvFile { get; set; }

    // Display (server-rendered data for initial filter dropdowns)
    public List<Expense> PreviewExpenses { get; set; } = new();
    public List<string> ExistingCategories { get; set; } = new();
    public List<string> AllTags { get; set; } = new();

    public bool ShowPreview { get; set; }
    public string? PreviewFileId { get; set; }
    public string? UploadMessage { get; set; }

    // Filter defaults (used to initialise the filter bar on first load)
    public string FilterDateFrom { get; set; } =
        new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");
    public string FilterDateTo { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string FilterCategory { get; set; } = string.Empty;
    public List<string> FilterTags { get; set; } = new();
}
