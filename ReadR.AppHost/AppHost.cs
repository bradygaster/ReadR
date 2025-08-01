using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

// Add the Azure Container App environment
builder.AddAzureContainerAppEnvironment("rdrenvironment");

// Add the Azure Application Insights environment
var readrinsights = builder.AddAzureApplicationInsights("readrinsights");

// Add all the Azure Storage accounts
var readrstorage = builder.AddAzureStorage("readrstorage")
                          .RunAsEmulator();

var webJobsStorage = builder.AddAzureStorage("AzureWebJobsStorage")
                            .RunAsEmulator();

var queues = readrstorage.AddQueues("queues");
var blobs = readrstorage.AddBlobs("blobs");

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
                      .WithExternalHttpEndpoints()
                      .WithReference(queues)
                      .WithReference(blobs)
                      .WithReference(readrinsights)
                      .WithRoleAssignments(readrstorage,
                            StorageBuiltInRole.StorageBlobDataOwner,
                            StorageBuiltInRole.StorageQueueDataContributor,
                            StorageBuiltInRole.StorageAccountContributor)
                      .WithRoleAssignments(webJobsStorage,
                            StorageBuiltInRole.StorageBlobDataOwner,
                            StorageBuiltInRole.StorageQueueDataContributor)
                      .PublishAsAzureContainerApp((resource, containerApp) =>
                      {
                      });

// functions project
var functions = builder.AddAzureFunctionsProject<Projects.ReadR_Serverless>("functions")
                       .WithHostStorage(webJobsStorage)
                       .WithReference(queues)
                       .WithReference(blobs)
                       .WithReference(readrinsights)
                       .WithRoleAssignments(readrstorage,
                            StorageBuiltInRole.StorageBlobDataOwner,
                            StorageBuiltInRole.StorageQueueDataContributor,
                            StorageBuiltInRole.StorageAccountContributor)
                       .WithRoleAssignments(webJobsStorage,
                            StorageBuiltInRole.StorageBlobDataOwner,
                            StorageBuiltInRole.StorageQueueDataContributor)
                       .PublishAsAzureContainerApp((resource, containerApp) =>
                       {
                       });

builder.Build().Run();
