namespace FinanceTracker.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Web.Models.ViewModels;
using FinanceTracker.Web.Services.Interfaces;

[Authorize]
public class TagsController : Controller
{
    private readonly ITagService _tagService;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagService tagService, ILogger<TagsController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var tags = await _tagService.GetTagsAsync(GetUserId());
        return View(new TagViewModel { Tags = tags });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(TagViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Tags = await _tagService.GetTagsAsync(GetUserId());
            return View("Index", model);
        }

        await _tagService.AddTagsAsync(GetUserId(), new[] { model.NewTag });
        _logger.LogInformation("User {UserId} added tag: {Tag}", GetUserId(), model.NewTag);
        TempData["SuccessMessage"] = $"Tag '{model.NewTag.Trim().ToLowerInvariant()}' added.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string tag)
    {
        await _tagService.DeleteTagAsync(GetUserId(), tag);
        _logger.LogInformation("User {UserId} deleted tag: {Tag}", GetUserId(), tag);
        TempData["SuccessMessage"] = $"Tag '{tag}' removed.";
        return RedirectToAction("Index");
    }

    /// <summary>AJAX autocomplete — returns matching tags as JSON array.</summary>
    [HttpGet]
    public async Task<IActionResult> Suggestions(string q = "")
    {
        var tags = await _tagService.SearchTagsAsync(GetUserId(), q);
        return Json(tags);
    }
}
