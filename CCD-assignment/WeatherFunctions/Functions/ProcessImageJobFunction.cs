using Azure;
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
    public async Task Run(
    [QueueTrigger("image-jobs")] string messageContent,
    CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<ImageJobMessageDto>(messageContent);
        if (message == null) return;

        try
        {
            _logger.LogInformation(
                "Processing image for Job: {JobId}, Station: {StationName}",
                message.JobId,
                message.WeatherStation.StationName);

            var result = await _imageService.GetAndProcessImageAsync(
                message.WeatherStation,
                message.JobId);

            bool success = false;
            int retryCount = 0;
            const int maxRetries = 10;

            while (!success && retryCount < maxRetries)
            {
                var record = await _jobStatusService.GetJobAsync(message.JobId);

                if (record == null) break;

                var urls = JsonSerializer.Deserialize<List<string>>(
                    record.ResultUrls) ?? new List<string>();

                string imageUrl = result.Uri.ToString();

                if (urls.Contains(imageUrl))
                {
                    _logger.LogInformation(
                        "Image already recorded for Job {JobId}: {Url}",
                        message.JobId, imageUrl);

                    success = true;
                    break;
                }

                urls.Add(imageUrl);

                record.ProcessedImages++;
                record.ResultUrls = JsonSerializer.Serialize(urls);

                try
                {
                    await _jobStatusService.UpdateProgressAsync(
                        record, cancellationToken);

                    _logger.LogInformation(
                        "Job {JobId} updated successfully. New Count: {Count}",
                        message.JobId,
                        record.ProcessedImages);

                    success = true;
                }
                catch (RequestFailedException ex) when (ex.Status == 412)
                {
                    retryCount++;

                    _logger.LogWarning(
                        "Concurrency conflict for Job {JobId}. Retry {RetryCount}/{MaxRetries}",
                        message.JobId,
                        retryCount,
                        maxRetries);

                    await Task.Delay(100 * retryCount, cancellationToken);
                }
                catch (Exception ex)
                {
                    retryCount++;

                    _logger.LogWarning(
                        ex,
                        "Update failed for Job {JobId}. Retry {RetryCount}/{MaxRetries}",
                        message.JobId,
                        retryCount,
                        maxRetries);

                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(100 * retryCount, cancellationToken);
                    }
                }
            }

            if (!success)
            {
                _logger.LogError(
                    "Failed to update job status for Job {JobId} after {MaxRetries} retries",
                    message.JobId,
                    maxRetries);

                throw new InvalidOperationException(
                    $"Failed to update job status for {message.JobId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing image for Job: {JobId}",
                message.JobId);

            throw;
        }
    }
}