using ReadR.Frontend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register HTTP client
builder.Services.AddHttpClient();

// Register feed source service (changed from StaticFeedSource to FileFeedSource)
builder.Services.AddSingleton<IFeedSource, FileFeedSource>();

// Register feed parser service  
builder.Services.AddScoped<IFeedParser, FeedParser>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<ReadR.Frontend.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();