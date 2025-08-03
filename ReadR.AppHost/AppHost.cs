var builder = DistributedApplication.CreateBuilder(args);

// functions project
var functions = builder.AddAzureFunctionsProject<Projects.ReadR_Serverless>("functions");

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend")
                      .WithExternalHttpEndpoints();

builder.Build().Run();
