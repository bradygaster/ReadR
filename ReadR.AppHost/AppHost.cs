var builder = DistributedApplication.CreateBuilder(args);

// front end project
var frontend = builder.AddProject<Projects.ReadR_Frontend>("frontend");

// functions project
var functions = builder.AddAzureFunctionsProject<Projects.ReadR_Serverless>("functions");

builder.Build().Run();
