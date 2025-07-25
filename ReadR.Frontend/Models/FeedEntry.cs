namespace ReadR.Frontend.Models;

public class FeedEntry
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime PublishDate { get; set; }
    public string Author { get; set; } = string.Empty;
    public string FeedSource { get; set; } = string.Empty;
    public string? FaviconUrl { get; set; }
    public string? FallbackIcon { get; set; }
    public List<string> Categories { get; set; } = new();
    public string? SourceCategory { get; set; }
    public string FeedDisplayName { get; set; } = string.Empty; 
    public string FeedUrl { get; set; } = string.Empty;
}
