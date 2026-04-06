namespace FinanceTracker.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetCategoriesAsync(GetUserId());
        return View(new CategoryViewModel { Categories = categories });
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string category)
    {
        await _categoryService.DeleteCategoryAsync(GetUserId(), category);
        _logger.LogInformation("User {UserId} deleted category: {Category}", GetUserId(), category);
        TempData["SuccessMessage"] = $"Category '{category}' removed.";
        return RedirectToAction("Index");
    }

    /// <summary>AJAX autocomplete — returns matching categories as JSON array.</summary>
    [HttpGet]
    public async Task<IActionResult> Suggestions(string q = "")
    {
        var categories = await _categoryService.SearchCategoriesAsync(GetUserId(), q);
        return Json(categories);
    }
}
