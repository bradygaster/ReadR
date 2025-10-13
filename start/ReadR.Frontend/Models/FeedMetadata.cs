using System.Text.RegularExpressions;

namespace ReadR.Frontend.Models;

public class FeedMetadata
{
    public string DisplayName { get; set; } = string.Empty;
    public string FaviconUrl { get; set; } = string.Empty;
    public string FallbackIcon { get; set; } = string.Empty;
    public bool IsWorking { get; set; }
    public DateTime LastChecked { get; set; }
    public string FeedUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets a URL-friendly slug based on the FeedUrl, removing protocol and non-alphanumeric characters
    /// </summary>
    public string GetSlug()
    {
        if (string.IsNullOrWhiteSpace(FeedUrl))
            return string.Empty;

        // Remove http:// or https://
        var cleanUrl = Regex.Replace(FeedUrl, @"^https?://", "", RegexOptions.IgnoreCase);

        // Keep only alphanumeric characters and replace sequences of non-alphanumeric with a single dash
        var slug = Regex.Replace(cleanUrl, @"[^a-zA-Z0-9]+", "-");

        // Remove leading/trailing dashes and convert to lowercase
        return slug.Trim('-').ToLowerInvariant();
    }
}
