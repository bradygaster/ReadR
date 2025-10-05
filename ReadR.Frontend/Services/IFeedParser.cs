using ReadR.Frontend.Models;

namespace ReadR.Shared.Services;

public interface IFeedParser
{
    Task<List<FeedEntry>> ParseFeedAsync(string feedUrl);
    Task<List<FeedEntry>> ParseFeedAsync(string feedUrl, string? sourceCategory);
}
