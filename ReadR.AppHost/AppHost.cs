var builder = DistributedApplication.CreateBuilder(args);

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
                       .WithEnvironment("readrqueues__queueServiceUri", readrqueues);

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
                      .WithExternalHttpEndpoints()
                      .WaitFor(readrqueues)
                      .WaitFor(readrblobs)
                      .WaitFor(functions)
                      .WithReference(readrqueues)
                      .WithReference(readrblobs)
                      .WithEnvironment("readrblobs__blobServiceUri", readrblobs)
                      .WithEnvironment("readrqueues__queueServiceUri", readrqueues);

builder.Build().Run();
