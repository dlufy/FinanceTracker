namespace FinanceTracker.Web.Services.Interfaces;

public interface ICategoryService
{
    Task<List<string>> GetCategoriesAsync(string userId);
    Task AddCategoriesAsync(string userId, IEnumerable<string> categories);
    Task DeleteCategoryAsync(string userId, string category);
    Task<List<string>> SearchCategoriesAsync(string userId, string query);

    /// <summary>
    /// Seeds the category store with the provided categories only when no category file exists for the user.
    /// One-time migration for users with pre-existing expense data.
    /// Does nothing if the category store file already exists.
    /// </summary>
    Task SeedCategoriesIfEmptyAsync(string userId, IEnumerable<string> categories);
}
