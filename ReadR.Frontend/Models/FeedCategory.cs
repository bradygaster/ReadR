namespace ReadR.Frontend.Models;

public class FeedCategory
{
    public string Name { get; set; } = string.Empty;
    public List<string> FeedUrls { get; set; } = new();
}

public class CategorizedFeeds
{
    public List<FeedCategory> Categories { get; set; } = new();
    
    /// <summary>
    /// Gets all feed URLs from all categories as a flat list
    /// </summary>
    public List<string> GetAllFeedUrls()
    {
        return Categories.SelectMany(c => c.FeedUrls).ToList();
    }
    
    /// <summary>
    /// Gets the category name for a specific feed URL
    /// </summary>
    public string? GetCategoryForFeedUrl(string feedUrl)
    {
        return Categories.FirstOrDefault(c => c.FeedUrls.Contains(feedUrl))?.Name;
    }
}