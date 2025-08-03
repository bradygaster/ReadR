using Microsoft.Extensions.Azure;
using ReadR.Frontend.Services;
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
    clientBuilder.AddBlobServiceClient(builder.Configuration["readrblobs:blobServiceUri"]!).WithName("readrblobs");
    clientBuilder.AddQueueServiceClient(builder.Configuration["readrblobs:queueServiceUri"]!).WithName("readrblobs");
    clientBuilder.AddTableServiceClient(builder.Configuration["readrblobs:tableServiceUri"]!).WithName("readrblobs");
    clientBuilder.AddBlobServiceClient(builder.Configuration["readrqueues:blobServiceUri"]!).WithName("readrqueues");
    clientBuilder.AddQueueServiceClient(builder.Configuration["readrqueues:queueServiceUri"]!).WithName("readrqueues");
    clientBuilder.AddTableServiceClient(builder.Configuration["readrqueues:tableServiceUri"]!).WithName("readrqueues");
});

// Register feed source service
// builder.Services.AddSingleton<IFeedSource, FileFeedSource>();
builder.Services.AddSingleton<IFeedSource, AzureBlobFeedSource>();

// Register feed parser service
builder.Services.AddScoped<IFeedParser, FeedParser>();

// Add new cache and page services
builder.Services.AddScoped<IFeedCacheService, FeedCacheService>();
builder.Services.AddScoped<IHomePageService, HomePageService>();

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

app.Run();
