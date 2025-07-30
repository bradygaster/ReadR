using ReadR.Frontend.Services;
using Microsoft.Extensions.Azure;
using ReadR.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Add memory cache
builder.Services.AddMemoryCache();

// Register HTTP client with timeout configuration
builder.Services.AddHttpClient<FeedParser>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "ReadR RSS Reader/1.0");
});

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["readrstorage:blobServiceUri"]!).WithName("readrstorage");
    clientBuilder.AddQueueServiceClient(builder.Configuration["readrstorage:queueServiceUri"]!).WithName("readrstorage");
    clientBuilder.AddTableServiceClient(builder.Configuration["readrstorage:tableServiceUri"]!).WithName("readrstorage");
});

// Register feed source service
// builder.Services.AddSingleton<IFeedSource, FileFeedSource>();
builder.Services.AddSingleton<IFeedSource, AzureBlobFeedSource>();

// Register feed parser service
builder.Services.AddScoped<IFeedParser, FeedParser>();

// Add new cache and page services
builder.Services.AddScoped<IFeedCacheService, FeedCacheService>();
builder.Services.AddScoped<IHomePageService, HomePageService>();
builder.Services.AddScoped<IQueueService, QueueService>();

// Add background service for queue monitoring
builder.Services.AddHostedService<QueueBackgroundService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<ReadR.Frontend.Components.App>().AddInteractiveServerRenderMode();

// provide an endpoint to refresh the feed cache
app.MapGet("/api/cache/refresh", async (IFeedCacheService feedCacheService) =>
{
    await feedCacheService.RefreshCacheAsync();
    return Results.Ok($"Feeds refreshed successfully at {DateTime.UtcNow}.");
});

// provide an endpoint to trigger a queue message for feed refresh
app.MapPost("/api/queue/refresh", async (IQueueService queueService) =>
{
    await queueService.SendFeedRefreshMessageAsync();
    return Results.Ok($"Feed refresh message sent to queue at {DateTime.UtcNow}.");
});

app.Run();
