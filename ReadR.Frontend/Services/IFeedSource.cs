using ReadR.Frontend.Models;

namespace ReadR.Frontend.Services;

public interface IFeedSource
{
    Task<List<string>> GetFeedUrlsAsync();
    Task<CategorizedFeeds> GetCategorizedFeedsAsync();
}