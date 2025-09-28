using ReadR.Frontend.Models;

namespace ReadR.Frontend.Services;

public interface IFeedManagementService
{
    /// <summary>
    /// Validates a feed URL by attempting to parse it
    /// </summary>
    /// <param name="feedUrl">The RSS/Atom feed URL to validate</param>
    /// <returns>A result indicating success or failure with error details</returns>
    Task<FeedValidationResult> ValidateFeedAsync(string feedUrl);

    /// <summary>
    /// Adds a new feed URL to the system
    /// </summary>
    /// <param name="feedUrl">The RSS/Atom feed URL to add</param>
    /// <param name="category">Optional category for the feed (defaults to "User Added")</param>
    /// <returns>A result indicating success or failure</returns>
    Task<FeedAddResult> AddFeedAsync(string feedUrl, string? category = null);

    /// <summary>
    /// Removes a feed URL from the system
    /// </summary>
    /// <param name="feedUrl">The RSS/Atom feed URL to remove</param>
    /// <returns>A result indicating success or failure</returns>
    Task<FeedRemoveResult> RemoveFeedAsync(string feedUrl);

    /// <summary>
    /// Checks if a feed URL already exists in the system
    /// </summary>
    /// <param name="feedUrl">The RSS/Atom feed URL to check</param>
    /// <returns>True if the feed already exists</returns>
    Task<bool> FeedExistsAsync(string feedUrl);
}

public class FeedValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public FeedMetadata? FeedMetadata { get; set; }
    public List<FeedEntry> SampleEntries { get; set; } = new();
}

public class FeedAddResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool AlreadyExists { get; set; }
}

public class FeedRemoveResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool NotFound { get; set; }
}
