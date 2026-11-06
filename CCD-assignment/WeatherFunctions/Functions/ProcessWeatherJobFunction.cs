using System.Text.Json;
using ExternalAPI.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WeatherFunctions.DTOs;
using WeatherFunctions.Services;

namespace WeatherFunctions.Functions;

public class ProcessWeatherJobFunction
{
    private readonly IWeatherService _weatherService;
    private readonly QueueStorageService _queueService;
    private readonly ILogger<ProcessWeatherJobFunction> _logger;

    public ProcessWeatherJobFunction(
        IWeatherService weatherService,
        QueueStorageService queueService,
        ILogger<ProcessWeatherJobFunction> logger)
    {
        _weatherService = weatherService;
        _queueService = queueService;
        _logger = logger;
    }

    [Function("ProcessWeatherJob")]
    public async Task Run(
        [QueueTrigger(
            QueueNames.WeatherJobs,
            Connection = "AzureWebJobsStorage")]
        string message,
        CancellationToken cancellationToken)
    {
        var job = JsonSerializer.Deserialize<WeatherJobMessageDto>(
            message);

        if (job == null || job.JobId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Invalid weather job message.");
        }

        _logger.LogInformation(
            "Starting weather job {JobId}",
            job.JobId);

        // Fetch weather data
        var stations = await _weatherService.GetStationsAsync(
            cancellationToken);

        if (stations.Count == 0)
        {
            throw new InvalidOperationException(
                "No weather stations were returned by Buienradar.");
        }

        _logger.LogInformation(
            "Retrieved {StationCount} weather stations for job {JobId}",
            stations.Count,
            job.JobId);

        // Fan out into individual image jobs
        foreach (var station in stations)
        {
            var imageJob = new ImageJobMessageDto
            {
                JobId = job.JobId,
                WeatherStation = station
            };

            _logger.LogInformation("Queuing image job for station: {StationName} (JobId: {JobId})",
                station.StationName, job.JobId);

            await _queueService.SendImageJobAsync(
                imageJob,
                cancellationToken);
        }

        _logger.LogInformation(
            "Created {StationCount} image jobs for job {JobId}",
            stations.Count,
            job.JobId);
    }
}