using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

// Add the Azure Container App environment
builder.AddAzureContainerAppEnvironment("readracaenv");

// azure storage
var storage = builder.AddAzureStorage("readrstorage")
                     .RunAsEmulator()
                     ;

var readrblobs = storage.AddBlobs("readrblobs");
var readrqueues = storage.AddQueues("readrqueues");

// functions project
var functions = builder.AddAzureFunctionsProject<Projects.ReadR_Serverless>("functions")
                       .WithHostStorage(storage)
                       .WaitFor(readrblobs)
                       .WaitFor(readrqueues)
                       .WithReference(readrblobs)
                       .WithReference(readrqueues)
                       .WithEnvironment("readrblobs__blobServiceUri", readrblobs)
                       .WithEnvironment("readrqueues__queueServiceUri", readrqueues)
                       .WithRoleAssignments(storage,
                            StorageBuiltInRole.StorageBlobDataOwner,
                            StorageBuiltInRole.StorageQueueDataContributor,
                            StorageBuiltInRole.StorageTableDataContributor
                        )
                       .PublishAsAzureContainerApp((aspireResource, containerApp) =>
                       {
                       })
                       ;

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
                      .WithExternalHttpEndpoints()
                      .WaitFor(readrqueues)
                      .WaitFor(readrblobs)
                      .WaitFor(functions)
                      .WithReference(readrqueues)
                      .WithEnvironment("readrblobs__blobServiceUri", readrblobs)
                      .WithEnvironment("readrqueues__queueServiceUri", readrqueues)
                      .WithReference(readrblobs)
                      .WithRoleAssignments(storage,
                           StorageBuiltInRole.StorageBlobDataOwner,
                           StorageBuiltInRole.StorageQueueDataContributor,
                           StorageBuiltInRole.StorageTableDataContributor
                      )
                      .PublishAsAzureContainerApp((aspireResource, containerApp) =>
                      {
                      })
                      ;

builder.Build().Run();
