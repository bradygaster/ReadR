using ReadR.Frontend.Models;
using ReadR.Shared.Services;

namespace ReadR.Frontend.Services;

public class FileFeedService : IFeedManagementService
{
    private readonly string _filePath;
    private readonly IFeedParser _feedParser;
    private readonly ILogger<FileFeedService> _logger;

    public FileFeedService(
        IFeedParser feedParser,
        ILogger<FileFeedService> logger)
    {
        _feedParser = feedParser;
        _logger = logger;
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "feed-urls.txt");
    }

    // Feed source methods (from IFeedSource)
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
                if (
                    Uri.TryCreate(trimmedLine, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                )
                {
                    currentCategory.FeedUrls.Add(trimmedLine);
                }
                else
                {
                    _logger.LogWarning("Invalid URL found in feed file: {Url}", trimmedLine);
                }
            }

            var totalUrls = categorizedFeeds.GetAllFeedUrls().Count;
            _logger.LogInformation(
                "Loaded {Count} feed URLs in {CategoryCount} categories from file: {FilePath}",
                totalUrls,
                categorizedFeeds.Categories.Count,
                _filePath
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read feed URLs from file: {FilePath}", _filePath);
        }

        return categorizedFeeds;
    }

    // Feed management methods
    public async Task<FeedValidationResult> ValidateFeedAsync(string feedUrl)
    {
        var result = new FeedValidationResult();

        try
        {
            // Normalize the URL
            if (!feedUrl.StartsWith("http://") && !feedUrl.StartsWith("https://"))
            {
                feedUrl = "https://" + feedUrl;
            }

            // Validate URL format
            if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                result.ErrorMessage = "Please enter a valid HTTP or HTTPS URL";
                return result;
            }

            _logger.LogDebug("Validating feed URL: {FeedUrl}", feedUrl);

            // Try to parse the feed
            var entries = await _feedParser.ParseFeedAsync(feedUrl, "User Added");

            if (entries.Count == 0)
            {
                result.ErrorMessage = "No feed entries found. Please check that the URL points to a valid RSS or Atom feed.";
                return result;
            }

            // Extract feed metadata from entries
            var firstEntry = entries.First();
            result.FeedMetadata = new FeedMetadata
            {
                DisplayName = firstEntry.FeedDisplayName,
                FaviconUrl = firstEntry.FaviconUrl ?? string.Empty,
                FallbackIcon = firstEntry.FallbackIcon ?? "🌐",
                IsWorking = true,
                LastChecked = DateTime.UtcNow,
                FeedUrl = feedUrl,
            };

            result.SampleEntries = entries.Take(3).ToList(); // Return first 3 entries as samples
            result.IsValid = true;

            _logger.LogDebug("Feed validation successful: {FeedUrl} - {DisplayName} ({EntryCount} entries)", 
                feedUrl, result.FeedMetadata.DisplayName, entries.Count);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogWarning(httpEx, "HTTP error validating feed: {FeedUrl}", feedUrl);
            result.ErrorMessage = "Unable to access the feed URL. Please check that the URL is correct and accessible.";
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout validating feed: {FeedUrl}", feedUrl);
            result.ErrorMessage = "The feed took too long to respond. Please try again or check that the URL is correct.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating feed: {FeedUrl}", feedUrl);
            result.ErrorMessage = "Unable to parse the feed. Please ensure the URL points to a valid RSS or Atom feed.";
        }

        return result;
    }

    public async Task<FeedAddResult> AddFeedAsync(string feedUrl, string? category = null)
    {
        var result = new FeedAddResult();
        category ??= "User Added";

        try
        {
            // First validate the feed
            var validation = await ValidateFeedAsync(feedUrl);
            if (!validation.IsValid)
            {
                result.ErrorMessage = validation.ErrorMessage;
                return result;
            }

            // Check if feed already exists
            if (await FeedExistsAsync(feedUrl))
            {
                result.AlreadyExists = true;
                result.ErrorMessage = "This feed has already been added to your collection.";
                return result;
            }

            // Add the feed to the local file
            await AddFeedToLocalFileAsync(feedUrl, category);

            result.Success = true;
            _logger.LogInformation("Successfully added feed: {FeedUrl} to category: {Category}", feedUrl, category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding feed: {FeedUrl}", feedUrl);
            result.ErrorMessage = "An error occurred while adding the feed. Please try again.";
        }

        return result;
    }

    public async Task<FeedRemoveResult> RemoveFeedAsync(string feedUrl)
    {
        var result = new FeedRemoveResult();

        try
        {
            // Check if feed exists
            if (!await FeedExistsAsync(feedUrl))
            {
                result.NotFound = true;
                result.ErrorMessage = "The specified feed was not found in your collection.";
                return result;
            }

            // Remove the feed from local file
            await RemoveFeedFromLocalFileAsync(feedUrl);

            result.Success = true;
            _logger.LogInformation("Successfully removed feed: {FeedUrl}", feedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing feed: {FeedUrl}", feedUrl);
            result.ErrorMessage = "An error occurred while removing the feed. Please try again.";
        }

        return result;
    }

    public async Task<bool> FeedExistsAsync(string feedUrl)
    {
        try
        {
            var categorizedFeeds = await GetCategorizedFeedsAsync();
            var allFeedUrls = categorizedFeeds.GetAllFeedUrls();
            
            // Normalize URLs for comparison
            var normalizedTargetUrl = NormalizeFeedUrl(feedUrl);
            return allFeedUrls.Any(url => NormalizeFeedUrl(url).Equals(normalizedTargetUrl, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if feed exists: {FeedUrl}", feedUrl);
            return false;
        }
    }

    // Private helper methods
    private async Task AddFeedToLocalFileAsync(string feedUrl, string category)
    {
        string currentContent = "";
        if (File.Exists(_filePath))
        {
            currentContent = await File.ReadAllTextAsync(_filePath);
        }

        var updatedContent = AddFeedToContent(currentContent, feedUrl, category);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        
        await File.WriteAllTextAsync(_filePath, updatedContent);
    }

    private async Task RemoveFeedFromLocalFileAsync(string feedUrl)
    {
        if (!File.Exists(_filePath))
            return;

        var currentContent = await File.ReadAllTextAsync(_filePath);
        var updatedContent = RemoveFeedFromContent(currentContent, feedUrl);
        
        await File.WriteAllTextAsync(_filePath, updatedContent);
    }

    private string AddFeedToContent(string content, string feedUrl, string category)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None).ToList();
        var categoryHeader = $"# {category}";
        
        // Find existing category or create new one
        var categoryIndex = lines.FindIndex(line => line.Trim().Equals(categoryHeader, StringComparison.OrdinalIgnoreCase));
        
        if (categoryIndex >= 0)
        {
            // Find insertion point after category header
            var insertIndex = categoryIndex + 1;
            
            // Skip to end of current category
            while (insertIndex < lines.Count && 
                   !string.IsNullOrWhiteSpace(lines[insertIndex]) && 
                   !lines[insertIndex].Trim().StartsWith('#'))
            {
                insertIndex++;
            }
            
            lines.Insert(insertIndex, feedUrl);
        }
        else
        {
            // Add new category at the end
            if (!string.IsNullOrWhiteSpace(content))
            {
                lines.Add("");
            }
            lines.Add(categoryHeader);
            lines.Add(feedUrl);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string RemoveFeedFromContent(string content, string feedUrl)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None).ToList();
        var normalizedTargetUrl = NormalizeFeedUrl(feedUrl);
        
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (!string.IsNullOrWhiteSpace(line) && 
                !line.StartsWith('#') && 
                NormalizeFeedUrl(line).Equals(normalizedTargetUrl, StringComparison.OrdinalIgnoreCase))
            {
                lines.RemoveAt(i);
                break;
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string NormalizeFeedUrl(string feedUrl)
    {
        try
        {
            var uri = new Uri(feedUrl.Trim());
            return uri.ToString().ToLowerInvariant();
        }
        catch
        {
            return feedUrl.Trim().ToLowerInvariant();
        }
    }
}
