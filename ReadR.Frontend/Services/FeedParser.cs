using System.ServiceModel.Syndication;
using System.Xml;
using ReadR.Frontend.Models;

namespace ReadR.Frontend.Services;

public class FeedParser : IFeedParser
{
    private readonly IFeedSource _feedSource;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FeedParser> _logger;

    public FeedParser(IFeedSource feedSource, HttpClient httpClient, ILogger<FeedParser> logger)
    {
        _feedSource = feedSource;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<FeedEntry>> ParseFeedAsync(string feedUrl)
    {
        var entries = new List<FeedEntry>();

        try
        {
            using var response = await _httpClient.GetAsync(feedUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var xmlReader = XmlReader.Create(stream);

            var feed = SyndicationFeed.Load(xmlReader);
            var feedTitle = feed.Title?.Text ?? ExtractDomainFromUrl(feedUrl);

            foreach (var item in feed.Items)
            {
                var entry = new FeedEntry
                {
                    Title = item.Title?.Text ?? "No Title",
                    Description = GetDescription(item),
                    Link = item.Links.FirstOrDefault()?.Uri.ToString() ?? string.Empty,
                    PublishDate = item.PublishDate.DateTime != DateTime.MinValue 
                        ? item.PublishDate.DateTime 
                        : item.LastUpdatedTime.DateTime,
                    Author = GetAuthor(item),
                    FeedSource = feedTitle,
                    Categories = item.Categories.Select(c => c.Name).ToList()
                };

                entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse feed: {FeedUrl}", feedUrl);
        }

        return entries;
    }

    public async Task<List<FeedEntry>> ParseAllFeedsAsync()
    {
        var allEntries = new List<FeedEntry>();
        var feedUrls = await _feedSource.GetFeedUrlsAsync();

        var tasks = feedUrls.Select(ParseFeedAsync);
        var results = await Task.WhenAll(tasks);

        foreach (var entries in results)
        {
            allEntries.AddRange(entries);
        }

        // Sort by publish date, newest first
        return allEntries.OrderByDescending(e => e.PublishDate).ToList();
    }

    private static string GetDescription(SyndicationItem item)
    {
        if (item.Summary?.Text != null)
            return item.Summary.Text;

        if (item.Content is TextSyndicationContent textContent)
            return textContent.Text;

        return string.Empty;
    }

    private static string GetAuthor(SyndicationItem item)
    {
        var author = item.Authors.FirstOrDefault();
        if (author != null)
        {
            return !string.IsNullOrEmpty(author.Name) ? author.Name : author.Email;
        }

        return string.Empty;
    }

    private static string ExtractDomainFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return "Unknown Source";
        }
    }
}