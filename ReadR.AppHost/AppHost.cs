using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

// Add the Application Insights telemetry
//var readrinsights = builder.AddAzureApplicationInsights("readrinsights");

// Add the Azure Container App environment
//builder.AddAzureContainerAppEnvironment("readracaenv");

// azure storage
var storage = builder.AddAzureStorage("readrstorage")
                     .RunAsEmulator()
                     ;

var readrblobs = storage.AddBlobs("readrblobs");
var readrqueues = storage.AddQueues("readrqueues");

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
                      .WithExternalHttpEndpoints()
                      .WaitFor(readrqueues)
                      .WaitFor(readrblobs)
                      .WithReference(readrqueues)
                      .WithEnvironment("readrblobs__blobServiceUri", readrblobs)
                      .WithEnvironment("readrqueues__queueServiceUri", readrqueues)
                      .WithReference(readrblobs)
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
