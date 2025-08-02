var builder = DistributedApplication.CreateBuilder(args);

// Add all the Azure Storage accounts
var readrstorage = builder.AddAzureStorage("readrstorage")
                          //.RunAsEmulator()
                          ;

var webJobsStorage = builder.AddAzureStorage("AzureWebJobsStorage")
                            //.RunAsEmulator()
                            ;

var queues = readrstorage.AddQueues("queues");
var blobs = readrstorage.AddBlobs("blobs");

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
                      .WithExternalHttpEndpoints()
                      .WaitFor(queues)
                      .WaitFor(blobs)
                      .WithReference(queues)
                      .WithReference(blobs);

// functions project
var functions = builder.AddAzureFunctionsProject<Projects.ReadR_Serverless>("functions")
                       .WithHostStorage(webJobsStorage)
                       .WaitFor(queues)
                       .WaitFor(blobs)
                       .WithReference(queues)
                       .WithReference(blobs);

builder.Build().Run();
