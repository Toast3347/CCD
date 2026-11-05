using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WeatherFunctions.DTOs;
using WeatherFunctions.Services;

namespace WeatherFunctions;

public class StartImageJobFunction
{
    private readonly QueueStorageService _queueService;

    private readonly JobStatusService _jobStatusService;

    private readonly ILogger<StartImageJobFunction> _logger;

    public StartImageJobFunction(
    QueueStorageService queueService,
    JobStatusService jobStatusService,
    ILogger<StartImageJobFunction> logger)
    {
        _queueService = queueService;
        _jobStatusService = jobStatusService;
        _logger = logger;
    }

    [Function("StartImageJob")]
    public async Task<HttpResponseData> Run(
    [HttpTrigger(
        AuthorizationLevel.Anonymous,
        "post",
        Route = "images")]
    HttpRequestData request,
    CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();

        try
        {
            await _jobStatusService.InitializeAsync(cancellationToken);

            await _jobStatusService.CreateJobAsync(
                jobId,
                cancellationToken);

            var message = new WeatherJobMessageDto
            {
                JobId = jobId
            };

            await _queueService.InitializeAsync(cancellationToken);

            await _queueService.SendWeatherJobAsync(
                message,
                cancellationToken);

            var baseUrl = request.Url.GetLeftPart(
                UriPartial.Authority);

            var response = request.CreateResponse(
                HttpStatusCode.Accepted);

            await response.WriteAsJsonAsync(new
            {
                JobId = jobId,
                Status = "Queued",
                CreatedAt = DateTimeOffset.UtcNow,
                Message = "Your image generation job has been accepted.",
                StatusUrl = $"{baseUrl}/api/images/{jobId}/status",
                ResultsUrl = $"{baseUrl}/api/images/{jobId}"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to start image generation job {JobId}",
                jobId);

            var response = request.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteAsJsonAsync(new
            {
                JobId = jobId,
                Status = "Failed",
                Message = "Could not start the image generation job."
            });

            return response;
        }
    }
}