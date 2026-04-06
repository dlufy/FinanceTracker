namespace FinanceTracker.Web.Services.Interfaces;

public interface ITagService
{
    Task<List<string>> GetTagsAsync(string userId);
    Task AddTagsAsync(string userId, IEnumerable<string> tags);
    Task DeleteTagAsync(string userId, string tag);
    Task<List<string>> SearchTagsAsync(string userId, string query);

    /// <summary>
    /// Seeds the tag store with the provided tags only when no tag file exists for the user yet.
    /// This is a one-time migration for users with pre-existing expense data.
    /// Does nothing if the tag store file already exists.
    /// </summary>
    Task SeedTagsIfEmptyAsync(string userId, IEnumerable<string> tags);
}
