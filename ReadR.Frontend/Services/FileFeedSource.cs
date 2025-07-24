using ReadR.Frontend.Models;

namespace ReadR.Frontend.Services;

public class FileFeedSource : IFeedSource
{
    private readonly string _filePath;
    private readonly ILogger<FileFeedSource> _logger;

    public FileFeedSource(ILogger<FileFeedSource> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "feed-urls.txt");
    }

    public async Task<List<string>> GetFeedUrlsAsync()
    {
        var categorizedFeeds = await GetCategorizedFeedsAsync();
        return categorizedFeeds.GetAllFeedUrls();
    }

    public async Task<CategorizedFeeds> GetCategorizedFeedsAsync()
    {
        var categorizedFeeds = new CategorizedFeeds();

        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogError("Feed URLs file not found at: {FilePath}", _filePath);
                return categorizedFeeds;
            }

            var lines = await File.ReadAllLinesAsync(_filePath);
            FeedCategory? currentCategory = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                // Check if this is a category header (starts with #)
                if (trimmedLine.StartsWith('#'))
                {
                    var categoryName = trimmedLine.Substring(1).Trim();
                    currentCategory = new FeedCategory { Name = categoryName };
                    categorizedFeeds.Categories.Add(currentCategory);
                    continue;
                }

                // If we don't have a current category, create a default one
                if (currentCategory == null)
                {
                    currentCategory = new FeedCategory { Name = "Uncategorized" };
                    categorizedFeeds.Categories.Add(currentCategory);
                }

                // Validate that the line is a valid URL
                if (Uri.TryCreate(trimmedLine, UriKind.Absolute, out var uri) && 
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    currentCategory.FeedUrls.Add(trimmedLine);
                }
                else
                {
                    _logger.LogWarning("Invalid URL found in feed file: {Url}", trimmedLine);
                }
            }

            var totalUrls = categorizedFeeds.GetAllFeedUrls().Count;
            _logger.LogInformation("Loaded {Count} feed URLs in {CategoryCount} categories from file: {FilePath}", 
                totalUrls, categorizedFeeds.Categories.Count, _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read feed URLs from file: {FilePath}", _filePath);
        }

        return categorizedFeeds;
    }
}