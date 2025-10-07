using Microsoft.Extensions.Azure;
using ReadR.Frontend.Services;
using ReadR.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
});

// Register unified feed management service (replaces both IFeedSource and IFeedManagementService)
// builder.Services.AddScoped<IFeedManagementService, FileFeedService>();
builder.Services.AddScoped<IFeedManagementService, AzureBlobFeedService>();

// Register feed parser service
builder.Services.AddScoped<IFeedParser, FeedParser>();

// Add new cache and page services
builder.Services.AddScoped<IFeedCacheService, FeedCacheService>();
builder.Services.AddScoped<IHomePageService, HomePageService>();

// Add markdown processing service
builder.Services.AddScoped<IMarkdownService, MarkdownService>();

// Add OpenAI and chat service
builder.AddAzureOpenAi();
builder.Services.AddSingleton<ChatService>();

// Application Insights telemetry configuration
//builder.Services.AddApplicationInsightsTelemetry(new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
//{
//    ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
//});
//builder.Services.AddApplicationInsightsTelemetry(new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
//{
//    ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
//});

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
