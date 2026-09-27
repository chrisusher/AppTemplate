var builder = DistributedApplication.CreateBuilder(args);

builder.Environment.ApplicationName = "App-Template";

// Parameters
var environment = builder.AddParameter("environment", false);

var cache = builder.AddRedis("cache");

// Storage
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithDataVolume("data");
        azurite.WithBlobPort(10000);
        azurite.WithQueuePort(10001);
        azurite.WithTablePort(10002);
        azurite.WithEndpoint("blob", endpoint => endpoint.IsProxied = false);
        azurite.WithEndpoint("queue", endpoint => endpoint.IsProxied = false);
        azurite.WithEndpoint("table", endpoint => endpoint.IsProxied = false);
    });

var dashboardOtlpEndpoint = builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"];
var otlpProtocol = string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]) ? "grpc" : builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];

var api = builder.AddAzureFunctionsProject("Backend", "../Backend/Backend.csproj")
    .WaitFor(storage)
    .WithArgs("", "--script-root", @"..\..\..")
    .WithEnvironment("Global__Environment", environment)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", dashboardOtlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otlpProtocol)
    .WithHostStorage(storage)
    .WithHttpHealthCheck("/api/health")
    .WithExternalHttpEndpoints();

builder.Build().Run();
