using Microsoft.Extensions.Caching.Memory;
using ReadR.Frontend.Models;

namespace ReadR.Frontend.Services;

public class FeedCacheService : IFeedCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IFeedParser _feedParser;
    private readonly IFeedSource _feedSource;
    private readonly ILogger<FeedCacheService> _logger;

    private const string CACHE_KEY = "all_feeds_data";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

    public FeedCacheService(
        IMemoryCache memoryCache,
        IFeedParser feedParser,
        IFeedSource feedSource,
        ILogger<FeedCacheService> logger
    )
    {
        _memoryCache = memoryCache;
        _feedParser = feedParser;
        _feedSource = feedSource;
        _logger = logger;
    }

    public async Task<CachedFeedData> GetCachedFeedsAsync()
    {
        if (
            _memoryCache.TryGetValue(CACHE_KEY, out CachedFeedData? cachedData)
            && cachedData != null
        )
        {
            _logger.LogDebug(
                "Retrieved feeds from cache. Last updated: {LastUpdated}",
                cachedData.LastUpdated
            );
            return cachedData;
        }

        _logger.LogInformation("Cache miss - loading feeds from source");
        return await LoadAndCacheFeedsAsync();
    }

    public async Task RefreshCacheAsync()
    {
        _logger.LogInformation("Refreshing feed cache");
        _memoryCache.Remove(CACHE_KEY);
        await LoadAndCacheFeedsAsync();
    }

    public async Task<List<FeedEntry>> GetFilteredEntriesAsync(
        string? categoryName = null,
        string? feedUrl = null
    )
    {
        var cachedData = await GetCachedFeedsAsync();
        var entries = cachedData.AllEntries;

        if (!string.IsNullOrEmpty(feedUrl))
        {
            string decodedFeedUrl;
            try
            {
                // Try base64 decoding first (new approach)
                var bytes = Convert.FromBase64String(feedUrl);
                decodedFeedUrl = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // Fallback to URL decoding (old approach)
                decodedFeedUrl = Uri.UnescapeDataString(feedUrl);
            }
            
            entries = entries.Where(e => e.FeedUrl == decodedFeedUrl).ToList();
        }
        else if (!string.IsNullOrEmpty(categoryName))
        {
            var decodedCategoryName = Uri.UnescapeDataString(categoryName);
            entries = entries
                .Where(e =>
                    string.Equals(
                        e.SourceCategory,
                        decodedCategoryName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToList();
        }

        return entries.OrderByDescending(e => e.PublishDate).ToList();
    }

    public async Task<CategorizedFeeds> GetWorkingFeedsAsync()
    {
        var cachedData = await GetCachedFeedsAsync();
        return cachedData.WorkingFeeds;
    }

    public async Task<string?> GetFeedUrlFromSlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var cachedData = await GetCachedFeedsAsync();
        
        // Search through all feed metadata to find the one with matching slug
        foreach (var kvp in cachedData.FeedMetadata)
        {
            if (kvp.Value.GetSlug() == slug)
            {
                return kvp.Key; // The key is the original feed URL
            }
        }

        return null;
    }

    private async Task<CachedFeedData> LoadAndCacheFeedsAsync()
    {
        try
        {
            var allCategorizedFeeds = await _feedSource.GetCategorizedFeedsAsync();
            var cachedData = new CachedFeedData { LastUpdated = DateTime.UtcNow };

            var allEntries = new List<FeedEntry>();
            var workingFeeds = new CategorizedFeeds();
            var feedMetadata = new Dictionary<string, FeedMetadata>();

            foreach (var category in allCategorizedFeeds.Categories)
            {
                var workingFeedUrls = new List<string>();

                foreach (var feedUrl in category.FeedUrls)
                {
                    try
                    {
                        _logger.LogDebug("Testing feed: {FeedUrl}", feedUrl);
                        var entries = await _feedParser.ParseFeedAsync(feedUrl, category.Name);

                        if (entries.Count > 0)
                        {
                            // Feed is working
                            workingFeedUrls.Add(feedUrl);
                            allEntries.AddRange(entries);

                            // Extract feed metadata from first entry
                            var firstEntry = entries.First();
                            feedMetadata[feedUrl] = new FeedMetadata
                            {
                                DisplayName = firstEntry.FeedDisplayName,
                                FaviconUrl = firstEntry.FaviconUrl ?? string.Empty,
                                FallbackIcon = firstEntry.FallbackIcon ?? "🌐",
                                IsWorking = true,
                                LastChecked = DateTime.UtcNow,
                                FeedUrl = feedUrl,
                            };

                            _logger.LogDebug(
                                "Feed working: {FeedUrl} - {EntryCount} entries",
                                feedUrl,
                                entries.Count
                            );
                        }
                        else
                        {
                            // Feed returned no entries
                            feedMetadata[feedUrl] = new FeedMetadata
                            {
                                DisplayName = ExtractDomainFromUrl(feedUrl),
                                FaviconUrl = GetSimpleFaviconUrl(feedUrl),
                                FallbackIcon = "🌐",
                                IsWorking = false,
                                LastChecked = DateTime.UtcNow,
                                FeedUrl = feedUrl,
                            };
                            _logger.LogWarning("Feed returned no entries: {FeedUrl}", feedUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Feed failed to parse
                        feedMetadata[feedUrl] = new FeedMetadata
                        {
                            DisplayName = ExtractDomainFromUrl(feedUrl),
                            FaviconUrl = GetSimpleFaviconUrl(feedUrl),
                            FallbackIcon = "❌",
                            IsWorking = false,
                            LastChecked = DateTime.UtcNow,
                            FeedUrl = feedUrl,
                        };
                        _logger.LogWarning(ex, "Failed to parse feed: {FeedUrl}", feedUrl);
                    }
                }

                // Only include categories that have at least one working feed
                if (workingFeedUrls.Count > 0)
                {
                    workingFeeds.Categories.Add(
                        new FeedCategory { Name = category.Name, FeedUrls = workingFeedUrls }
                    );
                }
            }

            cachedData.AllEntries = allEntries.OrderByDescending(e => e.PublishDate).ToList();
            cachedData.WorkingFeeds = workingFeeds;
            cachedData.FeedMetadata = feedMetadata;

            // Cache the data
            _memoryCache.Set(
                CACHE_KEY,
                cachedData,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheExpiry,
                    Priority = CacheItemPriority.High,
                }
            );

            _logger.LogInformation(
                "Cached {EntryCount} entries from {WorkingFeedCount} working feeds",
                allEntries.Count,
                feedMetadata.Values.Count(m => m.IsWorking)
            );

            return cachedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load and cache feeds");

            // Return empty cache data on error
            var errorData = new CachedFeedData { LastUpdated = DateTime.UtcNow };

            _memoryCache.Set(CACHE_KEY, errorData, TimeSpan.FromMinutes(5)); // Short cache on error
            return errorData;
        }
    }

    private static string GetSimpleFaviconUrl(string feedUrl)
    {
        try
        {
            var uri = new Uri(feedUrl);
            return $"https://{uri.Host}/favicon.ico";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ExtractDomainFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();

            // Remove www. prefix
            if (host.StartsWith("www."))
                host = host[4..];

            return host;
        }
        catch
        {
            return "Unknown Feed";
        }
    }
}
