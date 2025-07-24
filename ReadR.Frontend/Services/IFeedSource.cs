namespace ReadR.Frontend.Services;

public interface IFeedSource
{
    Task<List<string>> GetFeedUrlsAsync();
}