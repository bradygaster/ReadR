using ReadR.Frontend.Models;

namespace ReadR.Frontend.ViewModels;

public class HomeViewModel
{
    public List<FeedEntry> Entries { get; set; } = new();
    public CategorizedFeeds Categories { get; set; } = new();
    public Dictionary<string, FeedMetadata> FeedMetadata { get; set; } = new();
    public string? CurrentCategory { get; set; }
    public string? CurrentFeedUrl { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }

    public List<FeedEntry> GetCurrentPageEntries(int entriesPerPage)
    {
        return Entries.Skip(CurrentPage * entriesPerPage).Take(entriesPerPage).ToList();
    }

    public string GetPageTitle()
    {
        if (!string.IsNullOrEmpty(CurrentFeedUrl))
        {
            var decodedFeedUrl = Uri.UnescapeDataString(CurrentFeedUrl);

            // Try to get the display name from cached metadata first
            if (FeedMetadata.TryGetValue(decodedFeedUrl, out var metadata))
            {
                return $"Posts from {metadata.DisplayName}";
            }

            // Fallback: look in entries for the display name
            var entriesFromFeed = Entries.Where(e => e.FeedUrl == decodedFeedUrl).FirstOrDefault();
            if (
                entriesFromFeed != null
                && !string.IsNullOrWhiteSpace(entriesFromFeed.FeedDisplayName)
            )
            {
                return $"Posts from {entriesFromFeed.FeedDisplayName}";
            }

            // Final fallback to domain extraction
            try
            {
                var uri = new Uri(decodedFeedUrl);
                return $"Posts from {uri.Host}";
            }
            catch
            {
                return "Feed Posts";
            }
        }

        if (!string.IsNullOrEmpty(CurrentCategory))
        {
            return $"{Uri.UnescapeDataString(CurrentCategory)} Posts";
        }

        return "Latest Posts";
    }

    public string GetPageSubtitle()
    {
        if (!string.IsNullOrEmpty(CurrentFeedUrl))
        {
            var decodedFeedUrl = Uri.UnescapeDataString(CurrentFeedUrl);

            // Try to get the display name from cached metadata first
            if (FeedMetadata.TryGetValue(decodedFeedUrl, out var metadata))
            {
                return $"Latest posts from {metadata.DisplayName}";
            }

            // Fallback: look in entries for the display name
            var entriesFromFeed = Entries.Where(e => e.FeedUrl == decodedFeedUrl).FirstOrDefault();
            if (
                entriesFromFeed != null
                && !string.IsNullOrWhiteSpace(entriesFromFeed.FeedDisplayName)
            )
            {
                return $"Latest posts from {entriesFromFeed.FeedDisplayName}";
            }

            return "Posts from this specific feed";
        }

        if (!string.IsNullOrEmpty(CurrentCategory))
        {
            return $"All posts from {Uri.UnescapeDataString(CurrentCategory)} category";
        }

        return "Stay up to date with the .NET community";
    }
}
