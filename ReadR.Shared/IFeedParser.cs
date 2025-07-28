using ReadR.Frontend.Models;

namespace ReadR.Frontend.Services;

public interface IFeedParser
{
    Task<List<FeedEntry>> ParseFeedAsync(string feedUrl);
    Task<List<FeedEntry>> ParseFeedAsync(string feedUrl, string? sourceCategory);
    Task<List<FeedEntry>> ParseAllFeedsAsync();
    Task<Dictionary<string, List<FeedEntry>>> ParseAllFeedsByCategoryAsync();
}
