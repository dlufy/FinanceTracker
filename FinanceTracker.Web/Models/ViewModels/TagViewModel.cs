namespace FinanceTracker.Web.Models.ViewModels;

using System.ComponentModel.DataAnnotations;

public class TagViewModel
{
    public List<string> Tags { get; set; } = new();

    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, ErrorMessage = "Tag must be 50 characters or fewer")]
    public string NewTag { get; set; } = string.Empty;
}
