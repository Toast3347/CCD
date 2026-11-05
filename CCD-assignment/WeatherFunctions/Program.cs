using ExternalAPI.Interfaces;
using ExternalAPI.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using WeatherFunctions.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register services
builder.Services.AddHttpClient<IWeatherService, BuienradarService>();

var connectionString =
    Environment.GetEnvironmentVariable("AzureWebJobsStorage")
    ?? throw new InvalidOperationException(
        "AzureWebJobsStorage is not configured.");

builder.Services.AddSingleton(
    new QueueStorageService(connectionString));

builder.Services.AddSingleton(
    new JobStatusService(connectionString));

// Register OpenTelemetry
if (!string.IsNullOrEmpty(
    Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();