using ReadR.Frontend.ViewModels;

namespace ReadR.Frontend.Services;

public interface IHomePageService
{
    Task<HomeViewModel> GetHomeViewModelAsync(
        string? categoryName = null,
        string? feedUrl = null,
        int page = 0
    );
    Task RefreshDataAsync();
}
