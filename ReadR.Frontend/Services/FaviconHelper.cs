namespace ReadR.Frontend.Services;

public static class FaviconHelper
{
    private static readonly Dictionary<string, string> KnownFavicons = new()
    {
        // Microsoft sites
        { "devblogs.microsoft.com", "https://devblogs.microsoft.com/favicon.ico" },
        { "docs.microsoft.com", "https://docs.microsoft.com/favicon.ico" },
        { "azure.microsoft.com", "https://azure.microsoft.com/favicon.ico" },
        { "techcommunity.microsoft.com", "https://techcommunity.microsoft.com/favicon.ico" },
        
        // Popular dev blogs
        { "scotthanselman.com", "https://scotthanselman.com/favicon.ico" },
        { "hanselman.com", "https://hanselman.com/favicon.ico" },
        { "ardalis.com", "https://ardalis.com/favicon.ico" },
        { "andrewlock.net", "https://andrewlock.net/favicon.ico" },
        { "strathweb.com", "https://strathweb.com/favicon.ico" },
        { "khalidabuhakmeh.com", "https://khalidabuhakmeh.com/favicon.ico" },
        { "code-maze.com", "https://code-maze.com/favicon.ico" },
        { "jimmybogard.com", "https://jimmybogard.com/favicon.ico" },
        { "exceptionnotfound.net", "https://exceptionnotfound.net/favicon.ico" },
        { "meziantou.net", "https://meziantou.net/favicon.ico" },
        
        // Tech companies
        { "blog.jetbrains.com", "https://blog.jetbrains.com/favicon.ico" },
        { "stackoverflow.blog", "https://stackoverflow.blog/favicon.ico" },
        { "github.blog", "https://github.blog/favicon.ico" },
        
        // .NET Foundation
        { "dotnetfoundation.org", "https://dotnetfoundation.org/favicon.ico" },
        { "nuget.org", "https://nuget.org/favicon.ico" },
        
        // YouTube - use real favicon, not emoji
        { "youtube.com", "https://youtube.com/favicon.ico" },
        { "youtu.be", "https://youtube.com/favicon.ico" }
    };

    public static string? GetKnownFaviconUrl(string feedUrl)
    {
        try
        {
            var domain = ExtractDomain(feedUrl);
            if (string.IsNullOrEmpty(domain))
                return null;

            // First, check our known favicons
            if (KnownFavicons.TryGetValue(domain, out var favicon))
                return favicon;

            // For any domain not in our known list, try multiple favicon strategies
            // Start with the most reliable: Google's favicon service with higher resolution
            return $"https://www.google.com/s2/favicons?domain={domain}&sz=64";
        }
        catch
        {
            return null;
        }
    }

    public static string GetFallbackIcon(string feedSource)
    {
        var lowerSource = feedSource.ToLowerInvariant();
        
        // Technology related
        if (lowerSource.Contains("microsoft")) return "🏢";
        if (lowerSource.Contains("azure")) return "☁️";
        if (lowerSource.Contains("dotnet") || lowerSource.Contains(".net")) return "⚙️";
        if (lowerSource.Contains("aspnet") || lowerSource.Contains("asp.net")) return "🌐";
        if (lowerSource.Contains("blazor")) return "🔥";
        if (lowerSource.Contains("maui")) return "📱";
        if (lowerSource.Contains("xamarin")) return "📱";
        if (lowerSource.Contains("visual studio")) return "🛠️";
        if (lowerSource.Contains("vscode")) return "📝";
        if (lowerSource.Contains("github")) return "🐙";
        if (lowerSource.Contains("stackoverflow")) return "📚";
        if (lowerSource.Contains("jetbrains")) return "🧠";
        if (lowerSource.Contains("nuget")) return "📦";
        
        // People
        if (lowerSource.Contains("hanselman")) return "👨‍💻";
        if (lowerSource.Contains("ardalis")) return "👨‍💻";
        if (lowerSource.Contains("khalid")) return "👨‍💻";
        if (lowerSource.Contains("andrew")) return "👨‍💻";
        if (lowerSource.Contains("jimmy")) return "👨‍💻";
        
        // Content types
        if (lowerSource.Contains("blog")) return "📝";
        if (lowerSource.Contains("news")) return "📰";
        if (lowerSource.Contains("tutorial")) return "📚";
        if (lowerSource.Contains("documentation")) return "📖";
        if (lowerSource.Contains("guide")) return "🗺️";
        if (lowerSource.Contains("tips")) return "💡";
        if (lowerSource.Contains("community")) return "👥";
        if (lowerSource.Contains("foundation")) return "🏛️";
        if (lowerSource.Contains("code") || lowerSource.Contains("dev")) return "💻";
        
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
