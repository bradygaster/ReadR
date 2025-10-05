using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ReadR.Frontend.Models;
using ReadR.Frontend.Services;
using ReadR.Frontend.ViewModels;
using Microsoft.Extensions.Logging;

namespace ReadR.Frontend.Components.Pages;

public partial class Home : IDisposable
{
    [Parameter] public string? FeedSlug { get; set; }

    private HomeViewModel viewModel = new();
    private DotNetObjectReference<Home>? objRef;
    private const int entriesPerPage = 6;
    
    // Dialog state
    private bool isAddFeedDialogVisible = false;
    private bool isKeyboardHelpVisible = false;
    
    // Keyboard navigation state
    private int selectedCardIndex = 0;
    private int totalCards = 0;

    private List<FeedEntry> CurrentPageEntries => viewModel.GetCurrentPageEntries(entriesPerPage);

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IHomePageService HomePageService { get; set; } = default!;
    [Inject] private IFeedManagementService FeedManagementService { get; set; } = default!;
    [Inject] private IFeedCacheService FeedCacheService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<Home> Logger { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            objRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("setupKeyboardNavigation", objRef);
            await LoadViewModel();
        }
        else
        {
            // Refresh card selection after re-render
            await JSRuntime.InvokeVoidAsync("refreshCardSelection");
        }
    }

    [JSInvokable]
    public async Task HandleKeyPress(string key)
    {
        // Close dialog with Escape key
        if (key == "Escape")
        {
            if (isAddFeedDialogVisible)
            {
                await HideAddFeedDialog();
                return;
            }
            if (isKeyboardHelpVisible)
            {
                await HideKeyboardHelp();
                return;
            }
        }
        
        // Show keyboard help
        if (key == "ShowHelp")
        {
            await ShowKeyboardHelp();
            return;
        }
        
        // Navigation keys (only when no dialogs are visible)
        if (!isAddFeedDialogVisible && !isKeyboardHelpVisible)
        {
            switch (key)
            {
                case "ArrowLeft":
                case "NavigatePrevious":
                    await ChangePage(viewModel.CurrentPage - 1);
                    break;
                case "ArrowRight":
                case "NavigateNext":
                    await ChangePage(viewModel.CurrentPage + 1);
                    break;
            }
        }
    }
    
    [JSInvokable]
    public void UpdateSelection(int cardIndex, int totalCount)
    {
        selectedCardIndex = cardIndex;
        totalCards = totalCount;
        // Note: We don't call StateHasChanged here as this is just for tracking
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
        
        // Reset selection to first card when data loads
        selectedCardIndex = 0;
        await JSRuntime.InvokeVoidAsync("refreshCardSelection");
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
        
        // Reset selection to first card when page changes
        selectedCardIndex = 0;
        await JSRuntime.InvokeVoidAsync("refreshCardSelection");
    }

    private async Task RetryLoad()
    {
        await HomePageService.RefreshDataAsync();
        await LoadViewModel();
    }

    // Dialog management methods
    private async Task ShowAddFeedDialog()
    {
        isAddFeedDialogVisible = true;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task HideAddFeedDialog()
    {
        isAddFeedDialogVisible = false;
        StateHasChanged();
        await Task.CompletedTask;
    }
    
    private async Task ShowKeyboardHelp()
    {
        isKeyboardHelpVisible = true;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task HideKeyboardHelp()
    {
        isKeyboardHelpVisible = false;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task HandleAddFeedSubmit(string feedUrl)
    {
        await JSRuntime.InvokeVoidAsync("console.log", "Home: HandleAddFeedSubmit called with URL: " + feedUrl);
        
        try
        {
            Logger.LogInformation("Adding feed: {FeedUrl}", feedUrl);
            
            // Add the feed using the feed management service
            var result = await FeedManagementService.AddFeedAsync(feedUrl.Trim());
            
            await JSRuntime.InvokeVoidAsync("console.log", $"Home: AddFeedAsync result - Success: {result.Success}, AlreadyExists: {result.AlreadyExists}");
            
            if (result.Success)
            {
                // Feed added successfully - manually refresh the cache and reload the view
                await FeedCacheService.RefreshCacheAsync();
                await LoadViewModel();
                await JSRuntime.InvokeVoidAsync("showToast", "success", "Feed added successfully!", $"Added {feedUrl}");
                Logger.LogInformation("Successfully added feed: {FeedUrl}", feedUrl);
                
                // Only hide dialog on success
                await HideAddFeedDialog();
                await JSRuntime.InvokeVoidAsync("console.log", "Home: Dialog closed after successful add");
            }
            else if (result.AlreadyExists)
            {
                // Feed already exists - show warning but don't close dialog
                await JSRuntime.InvokeVoidAsync("showToast", "warning", "Feed already exists", "This feed is already in your collection.");
                Logger.LogWarning("Attempted to add existing feed: {FeedUrl}", feedUrl);
                await JSRuntime.InvokeVoidAsync("console.log", "Home: Feed already exists, keeping dialog open");
            }
            else
            {
                // Error adding feed - show error but don't close dialog
                await JSRuntime.InvokeVoidAsync("showToast", "error", "Failed to add feed", result.ErrorMessage ?? "An unknown error occurred.");
                Logger.LogError("Failed to add feed: {FeedUrl} - {Error}", feedUrl, result.ErrorMessage);
                await JSRuntime.InvokeVoidAsync("console.log", "Home: Error adding feed, keeping dialog open: " + (result.ErrorMessage ?? "Unknown error"));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception adding feed: {FeedUrl}", feedUrl);
            await JSRuntime.InvokeVoidAsync("showToast", "error", "Error adding feed", "An unexpected error occurred. Please try again.");
            await JSRuntime.InvokeVoidAsync("console.log", "Home: Exception in HandleAddFeedSubmit: " + ex.Message);
            // Don't close dialog on exception so user can try again
        }
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
