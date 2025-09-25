using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ReadR.Frontend.Models;
using ReadR.Frontend.Services;
using ReadR.Frontend.ViewModels;

namespace ReadR.Frontend.Components.Pages;

public partial class Home : IDisposable
{
    [Parameter] public string? FeedSlug { get; set; }

    private HomeViewModel viewModel = new();
    private DotNetObjectReference<Home>? objRef;
    private const int entriesPerPage = 9;

    private List<FeedEntry> CurrentPageEntries => viewModel.GetCurrentPageEntries(entriesPerPage);

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IHomePageService HomePageService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            objRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("setupKeyboardNavigation", objRef);
            await LoadViewModel();
        }
    }

    [JSInvokable]
    public async Task HandleKeyPress(string key)
    {
        switch (key)
        {
            case "ArrowLeft":
                await ChangePage(viewModel.CurrentPage - 1);
                break;
            case "ArrowRight":
                await ChangePage(viewModel.CurrentPage + 1);
                break;
        }
    }

    public void Dispose()
    {
        objRef?.Dispose();
        try { JSRuntime.InvokeVoidAsync("cleanupKeyboardNavigation"); } catch { }
    }

    private async Task LoadViewModel()
    {
        viewModel.IsLoading = true;
        StateHasChanged();

        viewModel = await HomePageService.GetHomeViewModelAsync(0);
        StateHasChanged();
    }

    private Task OnPageChangedAsync(int newPage) => ChangePage(newPage);

    private async Task ChangePage(int newPage)
    {
        if (newPage < 0
            || (viewModel.TotalPages > 0 && newPage >= viewModel.TotalPages)
            || newPage == viewModel.CurrentPage)
            return;

        viewModel = await HomePageService.GetHomeViewModelAsync(newPage);
        StateHasChanged();
    }

    private async Task RetryLoad()
    {
        await HomePageService.RefreshDataAsync();
        await LoadViewModel();
    }

    private string GetFeedDisplayName(string feedUrl)
    {
        if (viewModel.FeedMetadata != null && viewModel.FeedMetadata.TryGetValue(feedUrl, out var metadata))
        {
            return metadata.DisplayName;
        }

        if (viewModel.Entries != null)
        {
            var entriesFromFeed = viewModel.Entries.FirstOrDefault(e => e.FeedUrl == feedUrl);
            if (entriesFromFeed != null && !string.IsNullOrWhiteSpace(entriesFromFeed.FeedDisplayName))
            {
                return entriesFromFeed.FeedDisplayName;
            }
        }

        try
        {
            var uri = new Uri(feedUrl);
            var host = uri.Host.ToLowerInvariant();
            if (host.StartsWith("www."))
                host = host[4..];
            return host;
        }
        catch
        {
            return "Unknown Feed";
        }
    }
}
