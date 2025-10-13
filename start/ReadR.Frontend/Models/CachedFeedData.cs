namespace ReadR.Frontend.Models;

public class CachedFeedData
{
    public List<FeedEntry> AllEntries { get; set; } = new();
    public CategorizedFeeds WorkingFeeds { get; set; } = new();
    public Dictionary<string, FeedMetadata> FeedMetadata { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
