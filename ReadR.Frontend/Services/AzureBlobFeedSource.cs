using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using ReadR.Frontend.Models;
using ReadR.Shared.Services;

namespace ReadR.Frontend.Services;

public class AzureBlobFeedSource : IFeedSource
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobFeedSource> _logger;
    private readonly string _containerName;
    private readonly string _blobName;

    public AzureBlobFeedSource(
        IAzureClientFactory<BlobServiceClient> azureClientFactory,
        ILogger<AzureBlobFeedSource> logger,
        IConfiguration configuration)
    {
        _blobServiceClient = azureClientFactory.CreateClient("readrstorage");
        _logger = logger;
        _containerName = configuration["Azure:Blob:FeedContainer"] ?? "feeds";
        _blobName = configuration["Azure:Blob:FeedFileName"] ?? "feed-urls.txt";
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
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(_blobName);

            // Check if the blob exists
            var exists = await blobClient.ExistsAsync();
            if (!exists.Value)
            {
                _logger.LogError("Feed URLs blob not found: {ContainerName}/{BlobName}", _containerName, _blobName);
                return categorizedFeeds;
            }

            // Download the blob content
            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToString();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

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
                    _logger.LogWarning("Invalid URL found in feed blob: {Url}", trimmedLine);
                }
            }

            var totalUrls = categorizedFeeds.GetAllFeedUrls().Count;
            _logger.LogInformation(
                "Loaded {Count} feed URLs in {CategoryCount} categories from blob: {ContainerName}/{BlobName}",
                totalUrls,
                categorizedFeeds.Categories.Count,
                _containerName,
                _blobName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read feed URLs from blob: {ContainerName}/{BlobName}", _containerName, _blobName);
        }

        return categorizedFeeds;
    }
}