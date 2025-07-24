namespace ReadR.Frontend.Services;

public interface IFaviconService
{
    Task<string?> GetFaviconUrlAsync(string feedUrl, string siteUrl);
    string GetFallbackIcon(string feedSource);
}
