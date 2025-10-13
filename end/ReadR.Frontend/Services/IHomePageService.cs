using ReadR.Frontend.ViewModels;

namespace ReadR.Frontend.Services;

public interface IHomePageService
{
    Task<HomeViewModel> GetHomeViewModelAsync(int page = 0);
    Task RefreshDataAsync();
}
