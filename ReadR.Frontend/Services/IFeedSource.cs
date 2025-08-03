using ReadR.Frontend.Models;

namespace ReadR.Shared.Services;

public interface IFeedSource
{
    Task<List<string>> GetFeedUrlsAsync();
    Task<CategorizedFeeds> GetCategorizedFeedsAsync();
}
