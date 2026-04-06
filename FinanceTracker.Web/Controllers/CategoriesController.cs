namespace FinanceTracker.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using FinanceTracker.Web.Models.ViewModels;
using FinanceTracker.Web.Services.Interfaces;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Returns the Categories management page.</summary>
    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetCategoriesAsync(GetUserId());
        return View(new CategoryViewModel { Categories = categories });
    }

    /// <summary>Adds a new category to the authenticated user's category list.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _categoryService.GetCategoriesAsync(GetUserId());
            return View("Index", model);
        }

        await _categoryService.AddCategoriesAsync(GetUserId(), new[] { model.NewCategory });
        _logger.LogInformation("User {UserId} added category: {Category}", GetUserId(), model.NewCategory);
        TempData["SuccessMessage"] = $"Category '{model.NewCategory.Trim()}' added.";
        return RedirectToAction("Index");
    }

    /// <summary>Deletes a category from the authenticated user's category list.</summary>
    /// <param name="category">The category name to delete.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string category)
    {
        await _categoryService.DeleteCategoryAsync(GetUserId(), category);
        _logger.LogInformation("User {UserId} deleted category: {Category}", GetUserId(), category);
        TempData["SuccessMessage"] = $"Category '{category}' removed.";
        return RedirectToAction("Index");
    }

    /// <summary>Returns category suggestions matching the given prefix for autocomplete.</summary>
    /// <param name="q">Search prefix (empty returns all categories).</param>
    /// <returns>Array of matching category name strings.</returns>
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation(Summary = "Category autocomplete suggestions (AJAX)", Tags = new[] { "Categories" })]
    [SwaggerResponse(200, "Matching category names", typeof(string[]))]
    public async Task<IActionResult> Suggestions(string q = "")
    {
        var categories = await _categoryService.SearchCategoriesAsync(GetUserId(), q);
        return Json(categories);
    }
}
