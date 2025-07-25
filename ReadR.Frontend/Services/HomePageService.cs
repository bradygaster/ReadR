using ReadR.Frontend.Models;
using ReadR.Frontend.ViewModels;

namespace ReadR.Frontend.Services;

public class HomePageService : IHomePageService
{
    private readonly IFeedCacheService _feedCacheService;
    private readonly ILogger<HomePageService> _logger;
    private const int EntriesPerPage = 9;

    public HomePageService(IFeedCacheService feedCacheService, ILogger<HomePageService> logger)
    {
        _feedCacheService = feedCacheService;
        _logger = logger;
    }

    public async Task<HomeViewModel> GetHomeViewModelAsync(
        string? categoryName = null,
        string? feedUrl = null,
        int page = 0
    )
    {
        try
        {
            _logger.LogDebug(
                "Loading home view model for category: {Category}, feed: {Feed}, page: {Page}",
                categoryName,
                feedUrl,
                page
            );

            var cachedData = await _feedCacheService.GetCachedFeedsAsync();
            var filteredEntries = await _feedCacheService.GetFilteredEntriesAsync(
                categoryName,
                feedUrl
            );

            var totalPages =
                filteredEntries.Count > 0
                    ? (int)Math.Ceiling((double)filteredEntries.Count / EntriesPerPage)
                    : 0;

            // Ensure page is within valid range
            page = Math.Max(0, Math.Min(page, totalPages - 1));

            return new HomeViewModel
            {
                Entries = filteredEntries,
                Categories = cachedData.WorkingFeeds,
                FeedMetadata = cachedData.FeedMetadata,
                CurrentCategory = categoryName,
                CurrentFeedUrl = feedUrl,
                CurrentPage = page,
                TotalPages = totalPages,
                IsLoading = false,
                ErrorMessage = null,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load home view model");

            return new HomeViewModel
            {
                IsLoading = false,
                ErrorMessage = "Unable to load feeds at this time. Please try again later.",
            };
        }
    }

    public async Task<HomeViewModel> GetHomeViewModelBySlugAsync(
        string? categoryName = null,
        string? feedSlug = null,
        int page = 0
    )
    {
        // Convert slug to feed URL if provided
        string? feedUrl = null;
        if (!string.IsNullOrWhiteSpace(feedSlug))
        {
            feedUrl = await _feedCacheService.GetFeedUrlFromSlugAsync(feedSlug);
            if (feedUrl == null)
            {
                _logger.LogWarning("Could not resolve feed slug: {Slug}", feedSlug);
            }
        }

        return await GetHomeViewModelAsync(categoryName, feedUrl, page);
    }

    public async Task RefreshDataAsync()
    {
        _logger.LogInformation("Refreshing feed data");
        await _feedCacheService.RefreshCacheAsync();
    }
}
