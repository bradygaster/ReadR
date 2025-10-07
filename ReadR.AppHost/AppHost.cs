using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

// Add the Application Insights telemetry
//var readrinsights = builder.AddAzureApplicationInsights("readrinsights");

// Add the Azure Container App environment
//builder.AddAzureContainerAppEnvironment("readracaenv");

// azure storage
var storage = builder.AddAzureStorage("readrstorage")
                     .RunAsEmulator();

var readrblobs = storage.AddBlobs("readrblobs");

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
                      .WithExternalHttpEndpoints()
                      .WaitFor(readrblobs)
                      .WithReference(readrblobs)
                      .WithEnvironment("readrblobs__blobServiceUri", readrblobs)
                      //.WithReference(readrinsights)
                      //.WithRoleAssignments(storage,
                      //     StorageBuiltInRole.StorageBlobDataOwner,
                      //     StorageBuiltInRole.StorageQueueDataContributor,
                      //     StorageBuiltInRole.StorageTableDataContributor
                      //)
                      //.PublishAsAzureContainerApp((aspireResource, containerApp) =>
                      //{
                      //})
                      ;

builder.Build().Run();
