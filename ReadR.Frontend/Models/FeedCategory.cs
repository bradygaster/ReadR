namespace ReadR.Frontend.Models;

public class FeedCategory
{
    public string Name { get; set; } = string.Empty;
    public List<string> FeedUrls { get; set; } = new();
}

public class CategorizedFeeds
{
    public List<FeedCategory> Categories { get; set; } = new();

    public List<string> GetAllFeedUrls()
    {
        return Categories.SelectMany(c => c.FeedUrls).ToList();
    }

    public string? GetCategoryForFeedUrl(string feedUrl)
    {
        return Categories.FirstOrDefault(c => c.FeedUrls.Contains(feedUrl))?.Name;
    }
}
