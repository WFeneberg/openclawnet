var builder = DistributedApplication.CreateBuilder(args);

var sqliteConnectionString = builder.Configuration["OpenClawNet:ConnectionStrings:DefaultConnection"]
    ?? "Data Source=openclawnet.db";
var ollamaEndpoint = builder.Configuration["OpenClawNet:Model:Endpoint"]
    ?? builder.Configuration["Model:Endpoint"]
    ?? "http://localhost:11434";

var gateway = builder.AddProject<Projects.OpenClawNet_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ConnectionStrings__DefaultConnection", sqliteConnectionString)
    .WithEnvironment("Model__Endpoint", ollamaEndpoint);

builder.AddProject<Projects.OpenClawNet_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithEnvironment("OpenClawNet__OllamaBaseUrl", ollamaEndpoint);

builder.Build().Run();
