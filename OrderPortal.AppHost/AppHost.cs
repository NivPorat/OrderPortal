using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// --- Infrastructure Resources ---
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var redis = builder.AddRedis("redis");
var messaging = builder.AddRabbitMQ("messaging");

// --- Databases ---
var orderDb = postgres.AddDatabase("DefaultConnection", "orderportaldb");
var productDb = postgres.AddDatabase("ProductConnection", "productportaldb");

// --- Management Tools ---
var adminer = builder.AddContainer("adminer", "adminer")
    .WithHttpEndpoint(targetPort: 8080, name: "http")
    .WithEnvironment("ADMINER_DEFAULT_SERVER", "postgres");

// --- Microservices & Projects ---
var apiService = builder.AddProject<Projects.OrderPortal_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(productDb)
    .WithReference(redis);

var orderService = builder.AddProject<Projects.OrderPortal_OrderService>("orderservice")
    .WithReference(apiService)
    .WithReference(orderDb)
    .WithReference(messaging)
    .WaitFor(postgres)
    .WaitFor(messaging);

var web = builder.AddProject<Projects.OrderPortal_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WithReference(orderService)
    .WaitFor(apiService)
    .WaitFor(orderService);

builder.Build().Run();