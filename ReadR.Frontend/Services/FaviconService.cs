using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ReadR.Frontend.Services;

public class FaviconService : IFaviconService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FaviconService> _logger;
    private readonly ConcurrentDictionary<string, string?> _faviconCache = new();

    private readonly Dictionary<string, string> _fallbackIcons = new()
    {
        // Technology related
        { "microsoft", "🏢" },
        { "azure", "☁️" },
        { "dotnet", "⚙️" },
        { ".net", "⚙️" },
        { "csharp", "💻" },
        { "c#", "💻" },
        { "aspnet", "🌐" },
        { "asp.net", "🌐" },
        { "blazor", "🔥" },
        { "maui", "📱" },
        { "xamarin", "📱" },
        { "visual studio", "🛠️" },
        { "vscode", "📝" },
        { "github", "🐙" },
        { "stackoverflow", "📚" },
        { "jetbrains", "🧠" },
        { "nuget", "📦" },
        
        // Content types
        { "blog", "📝" },
        { "news", "📰" },
        { "tutorial", "📚" },
        { "documentation", "📖" },
        { "guide", "🗺️" },
        { "tips", "💡" },
        { "community", "👥" },
        { "foundation", "🏛️" },
        
        // Default fallback
        { "default", "🌐" }
    };

    public FaviconService(HttpClient httpClient, ILogger<FaviconService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure HttpClient for favicon requests
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ReadR/1.0 (Favicon Fetcher)");
        }
    }

    public async Task<string?> GetFaviconUrlAsync(string feedUrl, string siteUrl)
    {
        try
        {
            var domain = ExtractDomain(feedUrl) ?? ExtractDomain(siteUrl);
            if (string.IsNullOrEmpty(domain))
                return null;

            // Check cache first
            if (_faviconCache.TryGetValue(domain, out var cachedFavicon))
                return cachedFavicon;

            // Try to fetch favicon
            var faviconUrl = await FetchFaviconAsync(domain);
            _faviconCache[domain] = faviconUrl;
            return faviconUrl;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get favicon for {FeedUrl}", feedUrl);
            return null;
        }
    }

    public string GetFallbackIcon(string feedSource)
    {
        if (string.IsNullOrEmpty(feedSource))
            return _fallbackIcons["default"];

        var lowerSource = feedSource.ToLowerInvariant();

        // Check for exact matches first
        foreach (var (key, icon) in _fallbackIcons)
        {
            if (lowerSource.Contains(key, StringComparison.OrdinalIgnoreCase))
                return icon;
        }

        // Check for partial matches
        if (lowerSource.Contains("microsoft") || lowerSource.Contains("ms"))
            return _fallbackIcons["microsoft"];
        
        if (lowerSource.Contains("dev") || lowerSource.Contains("developer"))
            return "👨‍💻";
            
        if (lowerSource.Contains("tech") || lowerSource.Contains("technology"))
            return "💻";
            
        if (lowerSource.Contains("code") || lowerSource.Contains("coding"))
            return "💻";

        return _fallbackIcons["default"];
    }

    private async Task<string?> FetchFaviconAsync(string domain)
    {
        try
        {
            // Try common favicon locations in order of preference
            var faviconUrls = new[]
            {
                // Google's favicon service (most reliable fallback)
                $"https://www.google.com/s2/favicons?domain={domain}&sz=32",
                // Standard favicon locations
                $"https://{domain}/favicon.ico",
                $"https://{domain}/favicon.png",
                $"https://{domain}/apple-touch-icon.png",
                $"https://{domain}/apple-touch-icon-180x180.png",
                $"https://{domain}/apple-touch-icon-152x152.png",
                $"https://{domain}/apple-touch-icon-120x120.png",
                $"https://{domain}/images/favicon.ico",
                $"https://{domain}/img/favicon.ico",
                $"https://{domain}/assets/favicon.ico"
            };

            foreach (var url in faviconUrls)
            {
                try
                {
                    _logger.LogDebug("Trying favicon URL: {Url}", url);
                    
                    using var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var contentType = response.Content.Headers.ContentType?.MediaType;
                        var contentLength = response.Content.Headers.ContentLength;
                        
                        // Check if it's actually an image and has content
                        if ((contentType?.StartsWith("image/") == true || url.Contains("google.com/s2/favicons")) 
                            && contentLength > 0)
                        {
                            _logger.LogDebug("Found favicon at: {Url}", url);
                            return url;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to fetch favicon from {Url}", url);
                    continue;
                }
            }

            _logger.LogDebug("No favicon found for domain: {Domain}", domain);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching favicon for domain: {Domain}", domain);
            return null;
        }
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
            // Try regex as fallback
            var match = Regex.Match(url, @"https?://(?:www\.)?([^/]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
        }
    }
}
