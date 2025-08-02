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
        BlobServiceClient blobServiceClient,
        ILogger<AzureBlobFeedSource> logger,
        IConfiguration configuration)
    {
        _blobServiceClient = azureClientFactory.CreateClient("readrblobs");
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
                _logger.LogInformation("Feed URLs blob not found: {ContainerName}/{BlobName}. Creating from local file.", _containerName, _blobName);
                
                // Try to create the blob from the local file
                await CreateBlobFromLocalFileAsync(blobClient);
                
                // Check again if the blob exists after creation
                exists = await blobClient.ExistsAsync();
                if (!exists.Value)
                {
                    _logger.LogError("Failed to create feed URLs blob from local file: {ContainerName}/{BlobName}", _containerName, _blobName);
                    return categorizedFeeds;
                }
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

    private async Task CreateBlobFromLocalFileAsync(Azure.Storage.Blobs.BlobClient blobClient)
    {
        try
        {
            var localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "feed-urls.txt");
            
            if (!File.Exists(localFilePath))
            {
                _logger.LogError("Local feed URLs file not found at: {FilePath}", localFilePath);
                return;
            }

            var content = await File.ReadAllTextAsync(localFilePath);
            await blobClient.UploadAsync(BinaryData.FromString(content), overwrite: false);
            
            _logger.LogInformation("Successfully created blob from local file: {FilePath}", localFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create blob from local file");
        }
    }
}