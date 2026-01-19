var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AzureAiCouncil_Api>("api")
	.WithExternalHttpEndpoints();

builder.Build().Run();
