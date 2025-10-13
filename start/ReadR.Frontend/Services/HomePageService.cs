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
        int page = 0
    )
    {
        try
        {
            var cachedData = await _feedCacheService.GetCachedFeedsAsync();
            var entries = await _feedCacheService.GetFilteredEntriesAsync();

            var totalPages =
                entries.Count > 0
                    ? (int)Math.Ceiling((double)entries.Count / EntriesPerPage)
                    : 0;

            // Ensure page is within valid range
            page = Math.Max(0, Math.Min(page, totalPages - 1));

            return new HomeViewModel
            {
                Entries = entries,
                FeedMetadata = cachedData.FeedMetadata,
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

    public async Task RefreshDataAsync()
    {
        _logger.LogInformation("Refreshing feed data");
        await _feedCacheService.RefreshCacheAsync();
    }
}
