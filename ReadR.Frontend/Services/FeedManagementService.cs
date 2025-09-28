using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using ReadR.Frontend.Models;
using ReadR.Shared.Services;

namespace ReadR.Frontend.Services;

public class FeedManagementService : IFeedManagementService
{
    private readonly IFeedParser _feedParser;
    private readonly IFeedSource _feedSource;
    private readonly IFeedCacheService _feedCacheService;
    private readonly ILogger<FeedManagementService> _logger;
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string _containerName;
    private readonly string _blobName;
    private readonly bool _useAzureStorage;

    public FeedManagementService(
        IFeedParser feedParser,
        IFeedSource feedSource,
        IFeedCacheService feedCacheService,
        ILogger<FeedManagementService> logger,
        IConfiguration configuration,
        IAzureClientFactory<BlobServiceClient>? azureClientFactory = null)
    {
        _feedParser = feedParser;
        _feedSource = feedSource;
        _feedCacheService = feedCacheService;
        _logger = logger;
        
        // Check if we're using Azure storage
        _useAzureStorage = _feedSource is AzureBlobFeedSource;
        
        if (_useAzureStorage && azureClientFactory != null)
        {
            _blobServiceClient = azureClientFactory.CreateClient("readrblobs");
            _containerName = configuration["Azure:Blob:FeedContainer"] ?? "feeds";
            _blobName = configuration["Azure:Blob:FeedFileName"] ?? "feed-urls.txt";
        }
        else
        {
            _containerName = string.Empty;
            _blobName = string.Empty;
        }
    }

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

            // Add the feed to the storage
            if (_useAzureStorage && _blobServiceClient != null)
            {
                await AddFeedToAzureStorageAsync(feedUrl, category);
            }
            else
            {
                await AddFeedToLocalFileAsync(feedUrl, category);
            }

            // Refresh the cache to include the new feed
            await _feedCacheService.RefreshCacheAsync();

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

            // Remove the feed from storage
            if (_useAzureStorage && _blobServiceClient != null)
            {
                await RemoveFeedFromAzureStorageAsync(feedUrl);
            }
            else
            {
                await RemoveFeedFromLocalFileAsync(feedUrl);
            }

            // Refresh the cache to reflect the removal
            await _feedCacheService.RefreshCacheAsync();

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
            var categorizedFeeds = await _feedSource.GetCategorizedFeedsAsync();
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

    private async Task AddFeedToAzureStorageAsync(string feedUrl, string category)
    {
        if (_blobServiceClient == null)
            throw new InvalidOperationException("Azure Blob Service Client is not initialized");

        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await blobContainerClient.CreateIfNotExistsAsync();

        var blobClient = blobContainerClient.GetBlobClient(_blobName);

        // Download current content
        string currentContent = "";
        if (await blobClient.ExistsAsync())
        {
            var response = await blobClient.DownloadContentAsync();
            currentContent = response.Value.Content.ToString();
        }

        // Add the new feed to the appropriate category
        var updatedContent = AddFeedToContent(currentContent, feedUrl, category);

        // Upload updated content
        await blobClient.UploadAsync(BinaryData.FromString(updatedContent), overwrite: true);
    }

    private async Task AddFeedToLocalFileAsync(string feedUrl, string category)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "feed-urls.txt");
        
        string currentContent = "";
        if (File.Exists(filePath))
        {
            currentContent = await File.ReadAllTextAsync(filePath);
        }

        var updatedContent = AddFeedToContent(currentContent, feedUrl, category);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        
        await File.WriteAllTextAsync(filePath, updatedContent);
    }

    private async Task RemoveFeedFromAzureStorageAsync(string feedUrl)
    {
        if (_blobServiceClient == null)
            throw new InvalidOperationException("Azure Blob Service Client is not initialized");

        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = blobContainerClient.GetBlobClient(_blobName);

        if (!await blobClient.ExistsAsync())
            return;

        var response = await blobClient.DownloadContentAsync();
        var currentContent = response.Value.Content.ToString();

        var updatedContent = RemoveFeedFromContent(currentContent, feedUrl);
        await blobClient.UploadAsync(BinaryData.FromString(updatedContent), overwrite: true);
    }

    private async Task RemoveFeedFromLocalFileAsync(string feedUrl)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "feed-urls.txt");
        
        if (!File.Exists(filePath))
            return;

        var currentContent = await File.ReadAllTextAsync(filePath);
        var updatedContent = RemoveFeedFromContent(currentContent, feedUrl);
        
        await File.WriteAllTextAsync(filePath, updatedContent);
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
