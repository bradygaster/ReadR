namespace ReadR.Frontend.Models;

public class FeedEntry
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime PublishDate { get; set; }
    public string Author { get; set; } = string.Empty;
    public string FeedSource { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
}