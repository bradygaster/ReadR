namespace ReadR.Frontend.Services;

public class StaticFeedSource : IFeedSource
{
    private static readonly List<string> FeedUrls = new()
    {
        // Microsoft Official Blogs
        "https://devblogs.microsoft.com/dotnet/feed/",
        "https://devblogs.microsoft.com/aspnet/feed/",
        "https://devblogs.microsoft.com/visualstudio/feed/",
        
        // Community Blogs - .NET MVPs and Experts
        "https://www.hanselman.com/blog/feed/rss",
        "https://ardalis.com/feed.xml",
        "https://blog.jetbrains.com/dotnet/feed/",
        "https://www.meziantou.net/feed.xml",
        "https://andrewlock.net/rss.xml",
        "https://www.strathweb.com/feed/",
        "https://blog.stephencleary.com/feeds/posts/default",
        "https://davidpine.net/index.xml",
        "https://www.kallemarjokorpi.fi/rss/",
        "https://nikiforovall.github.io/feed.xml",
        
        // .NET Foundation and Community
        "https://dotnetfoundation.org/blog/feed",
        
        // Popular .NET Bloggers
        "https://khalidabuhakmeh.com/feed.xml",
        "https://code-maze.com/feed/",
        "https://www.c-sharpcorner.com/rss/members/latest-articles",
        "https://blog.jetbrains.com/dotnet/feed/",
        "https://jimmybogard.com/feed/",
        "https://exceptionnotfound.net/feed/",
        
        // Aspire and Cloud Native
        "https://techcommunity.microsoft.com/plugins/custom/microsoft/o365/custom-blog-rss?board=AppModernization",
        "https://azure.microsoft.com/en-us/blog/feed/",
        "https://aspireify.net/rss",
        
        // YouTube Channels (RSS)
        "https://www.youtube.com/feeds/videos.xml?channel_id=UCvtT19MZW8dq5Wwfu6B0oxw", // .NET
        "https://www.youtube.com/feeds/videos.xml?channel_id=UC-ptWR16ITQyYOglXyQmpzw", // Microsoft Developer
        "https://www.youtube.com/feeds/videos.xml?playlist_id=PLdo4fOcmZ0oUaQPBuCHVscl4OC7E6EMTn", // Aspire Fridays,

        // GitHub Feeds
        "https://github.com/dotnet/aspire/discussions.atom", // Aspire discussions
        "https://github.com/dotnet/aspire/releases.atom", // Aspire releases

    };

    public Task<List<string>> GetFeedUrlsAsync()
    {
        return Task.FromResult(new List<string>(FeedUrls));
    }
}