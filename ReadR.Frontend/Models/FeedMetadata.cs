namespace ReadR.Frontend.Models;

public class FeedMetadata
{
    public string DisplayName { get; set; } = string.Empty;
    public string FaviconUrl { get; set; } = string.Empty;
    public string FallbackIcon { get; set; } = string.Empty;
    public bool IsWorking { get; set; }
    public DateTime LastChecked { get; set; }
    public string FeedUrl { get; set; } = string.Empty;
}
