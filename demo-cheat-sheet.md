# ReadR Demo Cheat Sheet for Presenters

This cheat sheet provides step-by-step instructions for presenting the ReadR RSS aggregation application demo. It shows the evolution from a simple Blazor web app to a cloud-native application using Visual Studio tooling and Azure services.

## Setup Instructions

Before presenting, run the setup script to prepare all demo phases:

**Linux/macOS:**
```bash
chmod +x setup-demo.sh
./setup-demo.sh
```

**Windows:**
```powershell
./setup-demo.ps1
```

This creates separate directories for each phase in `demo-phases/` with clean user secrets.

## Presentation Overview

The demo shows 5 progressive phases:
1. **Phase 1**: Basic Blazor Server web app with memory caching
2. **Phase 2**: Azure Storage integration via Connected Services
3. **Phase 3**: Azure Functions with blob triggers and queue messaging
4. **Phase 4**: .NET Aspire orchestration for improved development experience
5. **Phase 5**: Azure deployment using `azd` with infrastructure as code

---

## Phase 1 - The ReadR Web App's Starting Point

**Directory**: `demo-phases/phase1-webapp-only/`

### What You're Demonstrating
- Starting point: a simple Blazor Server application
- Memory caching to prevent excessive RSS feed requests
- Azure App Service deployment using Visual Studio publish

### Key Files to Show
- `ReadR.Frontend/Components/Pages/Home.razor` - Main page component
- `ReadR.Frontend/Services/FileFeedSource.cs` - Hardcoded feed lists
- `ReadR.Frontend/Services/RssFeedService.cs` - RSS parsing and caching

### Demo Script

1. **Open the solution**:
   - Open `ReadR.slnx` in Visual Studio
   - Show the simple structure: just a web app project

2. **Run the application locally**:
   - Press F5 to run
   - Show the RSS feeds being displayed
   - Explain memory caching prevents repeated requests

3. **Show the code structure**:
   - Open `Components/Pages/Home.razor`
   - Point out the `@inject IRssFeedService RssFeedService`
   - Show the `@foreach` loop rendering feeds
   
4. **Examine the feed source**:
   - Open `Services/FileFeedSource.cs`
   - Show hardcoded feed lists in `GetFeedListsAsync()`
   - Point out this is not scalable for production

5. **Deploy to Azure**:
   - Right-click `ReadR.Frontend` project
   - Select "Publish..."
   - Choose "Azure App Service (Linux)" or "Azure App Service (Windows)"
   - Walk through the publish wizard to create a new App Service
   - **Generate GitHub Actions Workflow**: When prompted, choose to generate a GitHub Actions workflow
   - Note: Current VS generates traditional auth; OIDC support coming in future updates

### Key Teaching Points
- Memory caching prevents excessive HTTP requests to RSS feeds
- GitHub Actions workflow generation with Visual Studio
- Azure App Service hosting for .NET applications
- Simple architecture to start with

---

## Phase 2 - Adding Azure Storage

**Directory**: `demo-phases/phase2-storage/`

### What You're Demonstrating
- Azure Storage integration using Visual Studio Connected Services
- Managed identity authentication (no connection strings!)
- Separation of concerns with shared library

### Key File Changes from Phase 1
- **NEW**: `ReadR.Frontend/Services/AzureBlobFeedSource.cs` - Replaces FileFeedSource
- **NEW**: `ReadR.Shared/` - New class library project
- **MOVED**: RSS parsing logic moved to `ReadR.Shared/Models/` and `ReadR.Shared/Services/`
- **UPDATED**: `ReadR.Frontend/Program.cs` - Add Azure Storage and new dependencies

### Demo Script

1. **Add Azure Storage via Connected Services**:
   - Right-click the `ReadR.Frontend` project in Solution Explorer
   - Select "Add" → "Connected Service"
   - Choose "Azure Storage"
   - Select or create a Storage Account in your Azure subscription
   - **Key Point**: The wizard configures both your application code AND secures the Azure resource
   - Notice no connection strings or secrets are stored in your project

2. **Show the code changes**:
   - Open `Services/AzureBlobFeedSource.cs` 
   - **Point out these specific code elements**:
     ```csharp
     private readonly BlobServiceClient _blobServiceClient;
     
     public AzureBlobFeedSource(
         IAzureClientFactory<BlobServiceClient> azureClientFactory,
         ILogger<AzureBlobFeedSource> logger,
         IConfiguration configuration)
     {
         _blobServiceClient = azureClientFactory.CreateClient("readrstorage");
         _logger = logger;
         _containerName = configuration["Azure:Blob:FeedContainer"] ?? "feeds";
         _blobName = configuration["Azure:Blob:FeedFileName"] ?? "feed-urls.txt";
     }
     ```
   - Show how `IAzureClientFactory` is used (configured by Connected Services)
   - Explain how feed lists are now read from blob storage instead of hardcoded files

3. **Show the Azure client factory extensions**:
   - **We added this file**: `ReadR.Frontend/AzureClientFactoryBuilderExtensions.cs`
   - **Point out this extension pattern**:
     ```csharp
     internal static class AzureClientFactoryBuilderExtensions
     {
         public static IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddBlobServiceClient(
             this AzureClientFactoryBuilder builder, 
             string serviceUriOrConnectionString, 
             bool preferMsi = true)
         {
             if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri? serviceUri))
             {
                 return builder.AddBlobServiceClient(serviceUri);
             }
             else
             {
                 return BlobClientBuilderExtensions.AddBlobServiceClient(builder, serviceUriOrConnectionString);
             }
         }
     }
     ```

4. **Update Program.cs registrations**:
   - Open `ReadR.Frontend/Program.cs`
   - **Show these specific additions**:
     ```csharp
     builder.Services.AddAzureClients(clientBuilder =>
     {
         clientBuilder.AddBlobServiceClient(builder.Configuration["readrstorage:blobServiceUri"]!).WithName("readrstorage");
         clientBuilder.AddQueueServiceClient(builder.Configuration["readrstorage:queueServiceUri"]!).WithName("readrstorage");
         clientBuilder.AddTableServiceClient(builder.Configuration["readrstorage:tableServiceUri"]!).WithName("readrstorage");
     });
     
     builder.Services.AddSingleton<IFeedSource, AzureBlobFeedSource>();
     ```

5. **Upload feed configuration**:
   - Open the file already in the blob container in Storage Explorer, edit, and Save it back to the container
   - **Example feed file structure** (markdown format):
     ```
     # Technology
     https://devblogs.microsoft.com/dotnet/feed/
     https://www.hanselman.com/blog/rss.xml
     
     # News
     https://feeds.feedburner.com/oreilly/radar
     ```

6. **Run and test**:
   - Press F5 to run locally
   - Show feeds loading from Azure Storage
   - Explain managed identity handles authentication automatically

### Key Teaching Points
- Visual Studio Connected Services simplifies Azure integration
- Managed identity eliminates connection string management
- Azure Storage for scalable configuration management

---

## Phase 3 - Triggering Updates When Feed List Changes

**Directory**: `demo-phases/phase3-function-blob-trigger/`

### What You're Demonstrating
- Event-driven architecture with Azure Functions
- Blob triggers for automatic processing
- Queue messaging for decoupled communication
- Background services for cache invalidation

### Key File Changes from Phase 2
- **NEW**: `ReadR.Serverless/` - Azure Functions project
- **NEW**: `ReadR.Serverless/FeedListChangedFunction.cs` - Blob trigger function
- **NEW**: `ReadR.Frontend/Services/QueueMonitorService.cs` - Background service
- **UPDATED**: `ReadR.Frontend/Program.cs` - Add queue client and background service

### Demo Script

1. **Create Azure Functions project**:
   - Right-click solution → "Add" → "New Project"
   - Choose "Azure Functions" template
   - Name it `ReadR.Serverless`
   - Select "Blob trigger" template

2. **Show the blob trigger function**:
   - Open `ReadR.Serverless/FeedListChangedFunction.cs`
   - **Highlight this specific code**:
     ```csharp
     [Function("FeedListChangedFunction")]
     public async Task Run([BlobTrigger("feed-lists/{name}", Connection = "AzureWebJobsStorage")] 
         Stream stream, string name, FunctionContext context)
     {
         var logger = context.GetLogger("FeedListChangedFunction");
         logger.LogInformation($"Feed list changed: {name}");
         
         // Send message to queue to refresh cache
         var queueClient = new QueueClient(connectionString, "cache-refresh");
         await queueClient.SendMessageAsync($"refresh-{name}");
     }
     ```

3. **Add queue monitoring to web app**:
   - Open `ReadR.Frontend/Services/QueueMonitorService.cs`
   - **Show this background service code**:
     ```csharp
     public class QueueMonitorService : BackgroundService
     {
         protected override async Task ExecuteAsync(CancellationToken stoppingToken)
         {
             while (!stoppingToken.IsCancellationRequested)
             {
                 var messages = await _queueClient.ReceiveMessagesAsync();
                 foreach (var message in messages.Value)
                 {
                     // Clear memory cache when refresh message received
                     _memoryCache.Remove("feeds");
                     await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                 }
                 await Task.Delay(5000, stoppingToken);
             }
         }
     }
     ```

4. **Update Program.cs for queue integration**:
   - Open `ReadR.Frontend/Program.cs`
   - **Add these specific registrations**:
     ```csharp
     builder.Services.AddHostedService<QueueMonitorService>();
     ```

5. **Deploy Functions to Azure**:
   - Right-click `ReadR.Serverless` project
   - Select "Publish..."
   - Choose "Azure" → "Azure Function App (Linux)"
   - Create new Function App or select existing
   - **Configure storage connection**: Use same storage account as web app

6. **Test the end-to-end flow**:
   - Update a feed list file in blob storage
   - Show Function execution in Azure Portal logs
   - Watch queue message being processed
   - Observe cache refresh in web application

### Key Teaching Points
- Event-driven architecture improves responsiveness
- Azure Functions provide serverless compute for triggers
- Queue messaging enables loose coupling between components
- Background services handle long-running tasks in web apps

---

## Phase 4 - Adding Aspire

**Directory**: `demo-phases/phase4-adding-aspire/`

### What You're Demonstrating
- .NET Aspire orchestration for improved local development
- Service discovery and dependency management
- Integrated dashboard for monitoring
- Simplified multi-service debugging

### Key File Changes from Phase 3
- **NEW**: `ReadR.AppHost/` - Aspire orchestration project
- **NEW**: `ReadR.ServiceDefaults/` - Shared service configurations
- **UPDATED**: All projects now reference `ReadR.ServiceDefaults`
- **UPDATED**: `ReadR.slnx` - New startup project is `ReadR.AppHost`

### Demo Script

1. **Add Aspire orchestration project**:
   - Right-click on the `ReadR.Frontend` project in Solution Explorer
   - Select "Add" → "Aspire Orchestration..."
   - This will create both the AppHost and ServiceDefaults projects automatically
   - For more information on this feature, see: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=windows&pivots=visual-studio#add-orchestration-projects

2. **Configure the AppHost**:
   - Open `ReadR.AppHost/Program.cs`
   - **Show this orchestration code**:
     ```csharp
     var builder = DistributedApplication.CreateBuilder(args);

     var storage = builder.AddAzureStorage("ReadRStorage");
     var blobs = storage.AddBlobs();
     var queues = storage.AddQueues();

     var serverless = builder.AddAzureFunctionsProject<Projects.ReadR_Serverless>("serverless")
         .WithReference(storage);

     var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
         .WithReference(blobs)
         .WithReference(queues);

     builder.Build().Run();
     ```

3. **Set AppHost as startup project** (may be needed):
   - Right-click `ReadR.AppHost` in Solution Explorer
   - Select "Set as Startup Project"
   - **Note**: Visual Studio's orchestration feature may set this automatically

5. **Run with Aspire orchestration**:
   - Press F5 to start all services
   - **Show the Aspire dashboard** that automatically opens
   - Point out:
     - Service status and health checks
     - Logs aggregation from all services
     - Metrics and tracing information
     - Resource management (storage, queues)

6. **Demonstrate debugging experience**:
   - Set breakpoints in both frontend and function projects
   - Trigger a blob change to hit function breakpoint
   - Show cross-service debugging capabilities

### Key Teaching Points
- .NET Aspire simplifies multi-service development
- Integrated dashboard provides unified monitoring
- Service discovery eliminates configuration complexity
- Enhanced debugging experience across microservices

---

## Phase 5 - Previewing Aspire Publishing Features

**Directory**: `demo-phases/phase5-deploying-with-aspire/`

### What You're Demonstrating
- Azure deployment using `azd` (Azure Developer CLI)
- Infrastructure as Code with Bicep templates
- Container Apps deployment
- Production configuration management

### Key File Changes from Phase 4
- **NEW**: `azure.yaml` - Azure Developer CLI configuration
- **NEW**: `infra/` directory - Bicep infrastructure templates
- **UPDATED**: Projects configured for container deployment

### Demo Script

1. **Initialize Azure Developer CLI**:
   - Open terminal in solution root
   - **Run these specific commands**:
     ```bash
     azd init
     ```
   - Choose "Use code in the current directory"
   - Select the Aspire project when prompted

2. **Show the generated azure.yaml**:
   - Open `azure.yaml` in the root
   - **Point out these key sections**:
     ```yaml
     name: readr
     metadata:
       template: readr@0.0.1-beta
     services:
       frontend:
         project: ./ReadR.Frontend
         host: containerapp
       serverless:
         project: ./ReadR.Serverless
         host: function
     ```

3. **Examine infrastructure templates**:
   - Show `infra/` directory structure
   - Open `infra/main.bicep`
   - **Highlight key resources**:
     - Container Apps Environment
     - Azure Storage Account
     - Function App
     - Container Registry

4. **Deploy to Azure**:
   - **Run these commands in sequence**:
     ```bash
     azd auth login
     azd provision
     azd deploy
     ```
   - Show the provisioning progress
   - Point out resource creation in Azure Portal

5. **Show deployed application**:
   - Get the URL from `azd` output
   - Browse to deployed application
   - Show it working with Azure resources
   - **Demonstrate these features**:
     - Feed lists loading from Azure Storage
     - Function triggers working
     - Queue processing active

6. **Show Azure resources**:
   - Open Azure Portal
   - Show the resource group created by `azd`
   - Point out:
     - Container Apps with deployed services
     - Storage account with proper configurations
     - Function App with blob triggers
     - Application Insights for monitoring

### Key Teaching Points
- `azd` provides end-to-end deployment from code to cloud
- Infrastructure as Code ensures consistent deployments
- Container Apps provide modern serverless hosting
- Integrated monitoring and logging in production

---

## Prerequisites for Presenters

- Visual Studio 2022 (latest version with Aspire workload)
- .NET 9 SDK
- Azure subscription with appropriate permissions
- Azure Developer CLI (`azd`) installed
- Git command line tools
- Docker Desktop (for Aspire local development)

## Tips for Successful Demos

1. **Practice the flow**: Run through each phase beforehand
2. **Have backup slides**: In case live demos fail
3. **Use multiple Azure subscriptions**: Avoid naming conflicts
4. **Clean up resources**: Use `azd down` after demos
5. **Check service quotas**: Ensure sufficient Azure limits
6. **Test internet connectivity**: Demos require good bandwidth

## Troubleshooting Common Issues

### "Storage account name already exists"
- Use `azd env set AZURE_STORAGE_ACCOUNT_NAME <unique-name>`

### Functions not triggering
- Check storage account connection strings
- Verify blob container exists and has proper permissions

### Aspire dashboard not opening
- Check if Docker Desktop is running
- Try `dotnet workload restore` if Aspire templates missing

### Azure deployment fails
- Run `azd auth login` again
- Check Azure subscription permissions
- Verify resource provider registrations