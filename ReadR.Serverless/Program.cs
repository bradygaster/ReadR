using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Queues;
using System.Reflection;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddUserSecrets(Assembly.GetAssembly(typeof(Program))!);

builder.ConfigureFunctionsWebApplication();

// Register QueueServiceClient for dependency injection
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("readrstorage");
    return new QueueServiceClient(connectionString);
});

//builder.Services
//    .AddApplicationInsightsTelemetryWorkerService()
//    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
