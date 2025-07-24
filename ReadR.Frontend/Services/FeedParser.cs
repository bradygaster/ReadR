using System.IO;
using System.ServiceModel.Syndication;
using System.Xml;
using ReadR.Frontend.Models;

namespace ReadR.Frontend.Services;

public class FeedParser : IFeedParser
{
    private readonly IFeedSource _feedSource;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FeedParser> _logger;

    public FeedParser(IFeedSource feedSource, HttpClient httpClient, ILogger<FeedParser> logger)
    {
        _feedSource = feedSource;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<FeedEntry>> ParseFeedAsync(string feedUrl)
    {
        return await ParseFeedAsync(feedUrl, null);
    }

    public async Task<List<FeedEntry>> ParseFeedAsync(string feedUrl, string? sourceCategory)
    {
        var entries = new List<FeedEntry>();

        try
        {
            using var response = await _httpClient.GetAsync(feedUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var xmlReader = XmlReader.Create(stream);

            var feed = SyndicationFeed.Load(xmlReader);
            var feedTitle = feed.Title?.Text ?? ExtractDomainFromUrl(feedUrl);

            foreach (var item in feed.Items)
            {
                var entry = new FeedEntry
                {
                    Title = item.Title?.Text ?? "No Title",
                    Description = GetDescription(item),
                    Link = item.Links.FirstOrDefault()?.Uri.ToString() ?? string.Empty,
                    PublishDate =
                        item.PublishDate.DateTime != DateTime.MinValue
                            ? item.PublishDate.DateTime
                            : item.LastUpdatedTime.DateTime,
                    Author = GetAuthor(item),
                    FeedSource = feedTitle,
                    Categories = item.Categories.Select(c => c.Name).ToList(),
                    SourceCategory = sourceCategory
                };

                // Get favicon information using static helper
                entry.FaviconUrl = FaviconHelper.GetOptimalFaviconUrl(feedUrl);
                entry.FallbackIcon = FaviconHelper.GetFallbackIcon(feedTitle);

                entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse feed: {FeedUrl}", feedUrl);
        }

        return entries;
    }

    public async Task<List<FeedEntry>> ParseAllFeedsAsync()
    {
        var allEntries = new List<FeedEntry>();
        var categorizedFeeds = await _feedSource.GetCategorizedFeedsAsync();

        var tasks = new List<Task<List<FeedEntry>>>();

        foreach (var category in categorizedFeeds.Categories)
        {
            foreach (var feedUrl in category.FeedUrls)
            {
                tasks.Add(ParseFeedAsync(feedUrl, category.Name));
            }
        }

        var results = await Task.WhenAll(tasks);

        foreach (var entries in results)
        {
            allEntries.AddRange(entries);
        }

        // Sort by publish date, newest first
        return allEntries.OrderByDescending(e => e.PublishDate).ToList();
    }

    public async Task<Dictionary<string, List<FeedEntry>>> ParseAllFeedsByCategoryAsync()
    {
        var allEntries = await ParseAllFeedsAsync();
        
        return allEntries
            .GroupBy(entry => entry.SourceCategory ?? "Uncategorized")
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.PublishDate).ToList());
    }

    private static string GetDescription(SyndicationItem item)
    {
        // List of description extraction strategies, ordered by preference
        var strategies = new List<Func<string>>
        {
            // 1. Content:encoded extension (WordPress, many blogs) - full content
            () =>
                TryGetExtensionElement(item, "encoded", "http://purl.org/rss/1.0/modules/content/"),
            // 2. YouTube-specific description handling
            () => TryGetYouTubeDescription(item),
            // 3. Media:description (YouTube, video feeds)
            () => TryGetExtensionElement(item, "description", "http://search.yahoo.com/mrss/"),
            // 4. Media:content description attribute
            () => TryGetMediaContentDescription(item),
            // 5. Full content from item.Content
            () => TryGetSyndicationContent(item),
            // 6. Summary/description tag (standard RSS/Atom)
            () => item.Summary?.Text ?? string.Empty,
            // 7. Alternative description extensions
            () => TryGetExtensionElement(item, "description", ""),
            () => TryGetExtensionElement(item, "desc", ""),
            // 8. Excerpt or snippet extensions
            () =>
                TryGetExtensionElement(item, "excerpt", "http://wordpress.org/export/1.2/excerpt/"),
            () => TryGetExtensionElement(item, "snippet", ""),
            // 9. Custom content fields
            () => TryGetExtensionElement(item, "fulltext", ""),
            () => TryGetExtensionElement(item, "body", ""),
            // 10. Try to extract from first text node of any extension
            () => TryGetFirstTextExtension(item),
        };

        // Try each strategy until we find content
        foreach (var strategy in strategies)
        {
            try
            {
                var content = strategy();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return CleanDescription(content);
                }
            }
            catch
            {
                // Continue to next strategy on any error
                continue;
            }
        }

        return string.Empty;
    }

    private static string TryGetYouTubeDescription(SyndicationItem item)
    {
        // YouTube feeds often have better descriptions in specific places
        // Try multiple YouTube-specific approaches

        // 1. Look for media:group extensions first, then media:description within them
        var mediaGroupExtensions = item.ElementExtensions.Where(x =>
            x.OuterName == "group" && x.OuterNamespace == "http://search.yahoo.com/mrss/"
        );

        foreach (var groupExtension in mediaGroupExtensions)
        {
            try
            {
                var reader = groupExtension.GetReader();
                var groupXml = reader.ReadOuterXml();

                // Look for media:description within the group
                var descMatch = System.Text.RegularExpressions.Regex.Match(
                    groupXml,
                    @"<media:description[^>]*>(.*?)</media:description>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                        | System.Text.RegularExpressions.RegexOptions.Singleline
                );

                if (descMatch.Success)
                {
                    var desc = System.Net.WebUtility.HtmlDecode(descMatch.Groups[1].Value).Trim();
                    if (!string.IsNullOrWhiteSpace(desc) && !IsUnhelpfulContent(desc))
                        return desc;
                }

                // Also try a more flexible approach - look for any description element
                var flexibleMatch = System.Text.RegularExpressions.Regex.Match(
                    groupXml,
                    @"<(?:media:)?description[^>]*>(.*?)</(?:media:)?description>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                        | System.Text.RegularExpressions.RegexOptions.Singleline
                );

                if (flexibleMatch.Success)
                {
                    var desc = System.Net.WebUtility.HtmlDecode(flexibleMatch.Groups[1].Value).Trim();
                    if (!string.IsNullOrWhiteSpace(desc) && !IsUnhelpfulContent(desc))
                        return desc;
                }
            }
            catch
            {
                continue;
            }
        }

        // 2. Look for yt:description extension
        var ytDescription = TryGetExtensionElement(
            item,
            "description",
            "http://www.youtube.com/xml/schemas/2015"
        );
        if (!string.IsNullOrWhiteSpace(ytDescription) && !IsUnhelpfulContent(ytDescription))
            return ytDescription;

        // 3. Look for direct media:description with actual content (not just channel ID)
        var mediaDesc = TryGetExtensionElement(
            item,
            "description",
            "http://search.yahoo.com/mrss/"
        );
        if (!string.IsNullOrWhiteSpace(mediaDesc) && !IsUnhelpfulContent(mediaDesc))
            return mediaDesc;

        // 4. Check for content in various media elements
        var mediaExtensions = item.ElementExtensions.Where(x =>
            x.OuterNamespace == "http://search.yahoo.com/mrss/"
        );

        foreach (var extension in mediaExtensions)
        {
            try
            {
                var reader = extension.GetReader();
                var xmlContent = reader.ReadOuterXml();

                // Look for any description elements within media extensions
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    xmlContent,
                    @"<(?:media:)?description[^>]*>(.*?)</(?:media:)?description>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                        | System.Text.RegularExpressions.RegexOptions.Singleline
                );

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var desc = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
                    if (!string.IsNullOrWhiteSpace(desc) && !IsUnhelpfulContent(desc))
                        return desc;
                }
            }
            catch
            {
                continue;
            }
        }

        return string.Empty;
    }

    private static string TryGetExtensionElement(
        SyndicationItem item,
        string elementName,
        string elementNamespace
    )
    {
        var extension = item.ElementExtensions.FirstOrDefault(x =>
            x.OuterName.Equals(elementName, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrEmpty(elementNamespace) || x.OuterNamespace == elementNamespace)
        );

        if (extension == null)
            return string.Empty;

        try
        {
            // Try different ways to extract the content
            var reader = extension.GetReader();
            if (reader.IsEmptyElement)
                return string.Empty;

            // Read as simple string value
            var content = reader.ReadElementContentAsString();
            if (!string.IsNullOrWhiteSpace(content))
                return content;

            // Try getting as object if string method didn't work
            return extension.GetObject<string>() ?? string.Empty;
        }
        catch
        {
            try
            {
                // Fallback: try to get the inner XML
                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(
                    stringWriter,
                    new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        ConformanceLevel = ConformanceLevel.Fragment,
                    }
                );
                extension.WriteTo(xmlWriter);
                xmlWriter.Flush();
                return stringWriter.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    private static string TryGetMediaContentDescription(SyndicationItem item)
    {
        var mediaExtensions = item.ElementExtensions.Where(x =>
            x.OuterNamespace == "http://search.yahoo.com/mrss/"
        );

        foreach (var extension in mediaExtensions)
        {
            try
            {
                var reader = extension.GetReader();
                while (reader.Read())
                {
                    if (reader.HasAttributes)
                    {
                        var description = reader.GetAttribute("description");
                        if (!string.IsNullOrWhiteSpace(description))
                            return description;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return string.Empty;
    }

    private static string TryGetSyndicationContent(SyndicationItem item)
    {
        if (item.Content == null)
            return string.Empty;

        // Handle TextSyndicationContent
        if (
            item.Content is TextSyndicationContent textContent
            && !string.IsNullOrWhiteSpace(textContent.Text)
        )
            return textContent.Text;

        // Handle XmlSyndicationContent
        if (item.Content is XmlSyndicationContent xmlContent)
        {
            try
            {
                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(
                    stringWriter,
                    new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        ConformanceLevel = ConformanceLevel.Fragment,
                    }
                );
                xmlContent.WriteTo(xmlWriter, "content", "");
                xmlWriter.Flush();
                return stringWriter.ToString();
            }
            catch
            {
                // Try alternative extraction method
                try
                {
                    var reader = xmlContent.GetReaderAtContent();
                    return reader.ReadOuterXml();
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        // Handle UrlSyndicationContent (less common)
        if (item.Content is UrlSyndicationContent urlContent)
        {
            return urlContent.Url?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string TryGetFirstTextExtension(SyndicationItem item)
    {
        // As a last resort, try to find any extension with meaningful text content
        foreach (var extension in item.ElementExtensions)
        {
            try
            {
                var reader = extension.GetReader();
                if (reader.IsEmptyElement)
                    continue;

                var content = reader.ReadElementContentAsString();
                if (!string.IsNullOrWhiteSpace(content) && content.Length > 20) // Only consider substantial content
                {
                    return content;
                }
            }
            catch
            {
                continue;
            }
        }

        return string.Empty;
    }

    private static string CleanDescription(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        // Basic cleanup - remove excessive whitespace and normalize line endings
        content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ").Trim();

        // Decode common HTML entities if present
        content = System.Net.WebUtility.HtmlDecode(content);

        // Filter out unhelpful content patterns
        if (IsUnhelpfulContent(content))
            return string.Empty;

        return content;
    }

    private static bool IsUnhelpfulContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return true;

        // YouTube channel IDs (like "UCvtT19MZW8dq5Wwfu6B0oxw")
        if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^UC[a-zA-Z0-9_-]{22}$"))
            return true;

        // Other YouTube ID patterns
        if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^[a-zA-Z0-9_-]{11}$")) // Video IDs
            return true;

        // Generic alphanumeric IDs (likely not human-readable descriptions)
        if (
            content.Length >= 20
            && content.Length <= 30
            && System.Text.RegularExpressions.Regex.IsMatch(content, @"^[a-zA-Z0-9_-]+$")
        )
            return true;

        // Very short content that's likely not a meaningful description
        if (content.Length < 10 && !content.Contains(" "))
            return true;

        // Common placeholder or empty content patterns
        var unhelpfulPatterns = new[]
        {
            @"^(no\s*description|none|n/a|null|undefined)$",
            @"^(\.{3,}|_{3,}|-{3,})$",
            @"^\s*$",
            @"^(test|example|placeholder|todo|tbd)$",
            @"^(subscribe|like|share|comment)$", // Common YouTube call-to-actions without context
            @"^www\.[a-zA-Z0-9-]+\.[a-zA-Z]{2,}$", // Just URLs
            @"^https?://[^\s]+$", // Just URLs
            @"^[A-Z]{2,}[0-9]+$", // All caps with numbers (likely IDs)
            @"^(coming\s*soon|more\s*info|details\s*to\s*follow)$",
        };

        foreach (var pattern in unhelpfulPatterns)
        {
            if (
                System.Text.RegularExpressions.Regex.IsMatch(
                    content,
                    pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                )
            )
                return true;
        }

        // Check for content that's mostly numbers or symbols
        var alphaCount = content.Count(c => char.IsLetter(c));
        var totalCount = content.Length;
        if (totalCount > 5 && (double)alphaCount / totalCount < 0.3) // Less than 30% letters
            return true;

        return false;
    }

    private static string GetAuthor(SyndicationItem item)
    {
        var author = item.Authors.FirstOrDefault();
        if (author != null)
        {
            return !string.IsNullOrEmpty(author.Name) ? author.Name : author.Email;
        }

        return string.Empty;
    }

    private static string ExtractDomainFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return "Unknown Source";
        }
    }

    private static string? GetSiteUrlFromFeed(SyndicationFeed feed)
    {
        // Try to get the main site URL from feed links
        var link = feed.Links.FirstOrDefault(l => l.RelationshipType == "alternate" || string.IsNullOrEmpty(l.RelationshipType));
        return link?.Uri.ToString();
    }

    private static string? GetSiteUrlFromEntry(FeedEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Link))
            return null;

        try
        {
            var uri = new Uri(entry.Link);
            return $"{uri.Scheme}://{uri.Host}";
        }
        catch
        {
            return null;
        }
    }
}
