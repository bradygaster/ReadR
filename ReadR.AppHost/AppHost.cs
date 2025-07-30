using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

// connections for the storage accounts
var readrstorage = builder.AddAzureStorage("readrstorage")
                          .RunAsEmulator();

var webJobsStorage = builder.AddAzureStorage("AzureWebJobsStorage")
                            .RunAsEmulator();

var queues = readrstorage.AddQueues("queues");
var blobs = readrstorage.AddBlobs("blobs");

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
                      .WithReference(queues)
                      .WithReference(blobs)
                      .WithRoleAssignments(readrstorage,
                           StorageBuiltInRole.StorageBlobDataOwner,
                           StorageBuiltInRole.StorageQueueDataContributor);

// functions project
var functions = builder.AddAzureFunctionsProject<Projects.ReadR_Serverless>("functions")
                       .WithHostStorage(webJobsStorage)
                       .WithReference(queues)
                       .WithReference(blobs)
                       .WithRoleAssignments(webJobsStorage,
                            StorageBuiltInRole.StorageBlobDataOwner,
                            StorageBuiltInRole.StorageQueueDataContributor,
                            StorageBuiltInRole.StorageTableDataContributor)
                       .WithRoleAssignments(readrstorage,
                            StorageBuiltInRole.StorageBlobDataOwner,
                            StorageBuiltInRole.StorageQueueDataContributor);

builder.Build().Run();
