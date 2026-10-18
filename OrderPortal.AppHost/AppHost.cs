var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.OrderPortal_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

var orderService = builder.AddProject<Projects.OrderPortal_OrderService>("orderservice")
    .WithReference(apiService);

var web = builder.AddProject<Projects.OrderPortal_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WithReference(orderService)
    .WaitFor(apiService)
    .WaitFor(orderService);

builder.Build().Run();