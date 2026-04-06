namespace FinanceTracker.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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

    /// <summary>Returns the Tags management page.</summary>
    public async Task<IActionResult> Index()
    {
        var tags = await _tagService.GetTagsAsync(GetUserId());
        return View(new TagViewModel { Tags = tags });
    }

    /// <summary>Adds a new tag to the authenticated user's tag list.</summary>
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

    /// <summary>Deletes a tag from the authenticated user's tag list.</summary>
    /// <param name="tag">The tag value to delete.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string tag)
    {
        await _tagService.DeleteTagAsync(GetUserId(), tag);
        _logger.LogInformation("User {UserId} deleted tag: {Tag}", GetUserId(), tag);
        TempData["SuccessMessage"] = $"Tag '{tag}' removed.";
        return RedirectToAction("Index");
    }

    /// <summary>Returns tag suggestions matching the given prefix for autocomplete.</summary>
    /// <param name="q">Search prefix (empty returns all tags).</param>
    /// <returns>Array of matching tag strings.</returns>
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation(Summary = "Tag autocomplete suggestions (AJAX)", Tags = new[] { "Tags" })]
    [SwaggerResponse(200, "Matching tag strings", typeof(string[]))]
    public async Task<IActionResult> Suggestions(string q = "")
    {
        var tags = await _tagService.SearchTagsAsync(GetUserId(), q);
        return Json(tags);
    }
}
