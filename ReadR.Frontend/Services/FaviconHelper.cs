namespace ReadR.Frontend.Services;

public static class FaviconHelper
{
    /// <summary>
    /// Gets the optimal favicon URL for a given feed URL using a multi-strategy approach
    /// </summary>
    public static string? GetOptimalFaviconUrl(string feedUrl)
    {
        try
        {
            var domain = ExtractDomain(feedUrl);
            if (string.IsNullOrEmpty(domain))
                return null;

            // For unknown domains, try the site's own favicon first
            return $"https://{domain}/favicon.ico";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all fallback favicon URLs for a domain in order of preference
    /// </summary>
    public static string[] GetFallbackFaviconUrls(string domain)
    {
        return new[]
        {
            $"https://{domain}/apple-touch-icon.png",
            $"https://{domain}/favicon.png",
            $"https://www.google.com/s2/favicons?domain={domain}&sz=32",
        };
    }

    public static string GetFallbackIcon(string feedSource)
    {
        var lowerSource = feedSource.ToLowerInvariant();

        // Content types
        if (lowerSource.Contains("blog"))
            return "📝";
        if (lowerSource.Contains("news"))
            return "📰";
        if (lowerSource.Contains("tutorial"))
            return "📚";
        if (lowerSource.Contains("documentation"))
            return "📖";
        if (lowerSource.Contains("guide"))
            return "🗺️";
        if (lowerSource.Contains("tips"))
            return "💡";
        if (lowerSource.Contains("community"))
            return "👥";
        if (lowerSource.Contains("foundation"))
            return "🏛️";
        if (lowerSource.Contains("code") || lowerSource.Contains("dev"))
            return "💻";

        // Default fallback
        return "🌐";
    }

    private static string? ExtractDomain(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

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
            return null;
        }
    }
}
