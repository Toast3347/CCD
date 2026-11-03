using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using ExternalAPI.Interfaces;
using ExternalAPI.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddHttpClient<IWeatherService, BuienradarService>();

builder.Services.AddSingleton(sp =>
{
    var connectionString =
        Environment.GetEnvironmentVariable("AzureWebJobsStorage")
        ?? throw new InvalidOperationException(
            "AzureWebJobsStorage is not configured.");

    return new QueueClient(
        connectionString,
        "weather-jobs",
        new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });
});

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
