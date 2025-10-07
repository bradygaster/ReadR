using Aspire.Hosting.Azure;
using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

// Since we may have an existing foundry instance
var existingFoundryName = builder.AddParameter("existingFoundryName");
var existingFoundryResourceGroup = builder.AddParameter("existingFoundryResourceGroup");
var model = builder.AddParameter("model", "gpt-4o", true, false);

// Add the Application Insights telemetry
var readrinsights = builder.AddAzureApplicationInsights("readrinsights");

// Add the Azure Container App environment
builder.AddAzureContainerAppEnvironment("readracaenv");

// azure storage
var storage = builder.AddAzureStorage("readrstorage").RunAsEmulator();
var readrblobs = storage.AddBlobs("readrblobs");

// ai foundry
//var foundry = builder.AddAzureAIFoundry("foundry").AsExisting(existingFoundryName, existingFoundryResourceGroup);
//var gpt4 = foundry.AddDeployment("gpt-4o", AIFoundryModel.OpenAI.Gpt4);

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
                      .WithExternalHttpEndpoints()
                      .WaitFor(readrblobs)
                      .WithReference(readrblobs)
                      .WithEnvironment("readrblobs__blobServiceUri", readrblobs)
                      .WithReference(readrinsights)
                      //.WithEnvironment("AZURE_OPENAI_ENDPOINT", foundry.Resource.AIFoundryApiEndpoint)
                      //.WithEnvironment("AZURE_OPENAI_MODEL_NAME", model)
                      //.WaitFor(gpt4)
                      .WithRoleAssignments(storage,
                           StorageBuiltInRole.StorageBlobDataOwner,
                           StorageBuiltInRole.StorageQueueDataContributor,
                           StorageBuiltInRole.StorageTableDataContributor
                      )
                      .PublishAsAzureContainerApp((aspireResource, containerApp) =>
                      {
                          containerApp.Template.Scale.MinReplicas = 1;
                          containerApp.Template.Scale.MaxReplicas = 2;
                      });

builder.Build().Run();
