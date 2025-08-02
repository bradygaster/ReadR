using ReadR.Frontend.Services;
using ReadR.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Add application insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("readrinsights");
});

// Add Azure Storage configuration
builder.AddAzureBlobServiceClient("blobs");
builder.AddAzureQueueServiceClient("queues");

// Add memory cache
builder.Services.AddMemoryCache();

// Register HTTP client with timeout configuration
builder.Services.AddHttpClient<FeedParser>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "ReadR RSS Reader/1.0");
});

// Register feed source service
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

app.MapDefaultEndpoints();

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
