using ExternalAPI.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WeatherFunctions.DTOs;
using WeatherFunctions.Services;

namespace WeatherFunctions.Functions;

public sealed class ProcessImageJobFunction
{
    private readonly ILogger _logger;
    private readonly IImageService _imageService;
    private readonly JobStatusService _jobStatusService;

    public ProcessImageJobFunction(
        ILoggerFactory loggerFactory,
        IImageService imageService,
        JobStatusService jobStatusService)
    {
        _logger = loggerFactory.CreateLogger<ProcessImageJobFunction>();
        _imageService = imageService;
        _jobStatusService = jobStatusService;
    }

    [Function("ProcessImageJob")]
    public async Task Run([QueueTrigger("image-jobs")] string messageContent)
    {
        var message = JsonSerializer.Deserialize<ImageJobMessageDto>(messageContent);
        if (message == null)
        {
            _logger.LogWarning("Received null message content.");
            return;
        }

        try
        {
            _logger.LogInformation("Processing image for Job: {JobId}, Station: {StationName}",
                message.JobId, message.WeatherStation.StationName);

            var result = await _imageService.GetAndProcessImageAsync(message.WeatherStation, message.JobId);

            var record = await _jobStatusService.GetJobAsync(message.JobId);
            if (record == null)
            {
                _logger.LogError("Job record {JobId} was not found in the table.", message.JobId);
                return;
            }

            var urls = JsonSerializer.Deserialize<List<string>>(record["ResultUrls"].ToString() ?? "[]")
                       ?? new List<string>();

            if (!urls.Contains(result.Uri.ToString()))
            {
                urls.Add(result.Uri.ToString());
            }

            int currentProcessed = record.ContainsKey("ProcessedImages") && record["ProcessedImages"] is JsonElement element
                ? element.GetInt32()
                : 0;

            await _jobStatusService.UpdateProgressAsync(
                message.JobId,
                currentProcessed + 1,
                0, 
                urls
            );

            _logger.LogInformation("Successfully completed image and updated status for Job: {JobId}", message.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image for Job: {JobId}", message.JobId);
            throw;
        }
    }
}
