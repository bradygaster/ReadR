using ReadR.Frontend.Models;

namespace ReadR.Frontend.Services;

public interface IFeedCacheService
{
    Task<CachedFeedData> GetCachedFeedsAsync();
    Task RefreshCacheAsync();
    Task<List<FeedEntry>> GetFilteredEntriesAsync(
        string? categoryName = null,
        string? feedUrl = null
    );
    Task<CategorizedFeeds> GetWorkingFeedsAsync();
    Task<string?> GetFeedUrlFromSlugAsync(string slug);
}
