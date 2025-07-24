using ReadR.Frontend.Models;

namespace ReadR.Frontend.Services;

public interface IFeedParser
{
    Task<List<FeedEntry>> ParseFeedAsync(string feedUrl);
    Task<List<FeedEntry>> ParseAllFeedsAsync();
}