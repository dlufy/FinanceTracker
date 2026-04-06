namespace FinanceTracker.Web.Models.ViewModels;

using System.ComponentModel.DataAnnotations;

public class CategoryViewModel
{
    public List<string> Categories { get; set; } = new();

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category must be 100 characters or fewer")]
    public string NewCategory { get; set; } = string.Empty;
}
