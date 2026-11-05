using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WeatherFunctions.Services;

namespace WeatherFunctions.Functions;

public sealed class GetImageJobStatusFunction
{
    private readonly JobStatusService _jobStatusService;
    private readonly ILogger<GetImageJobStatusFunction> _logger;

    public GetImageJobStatusFunction(
        JobStatusService jobStatusService,
        ILogger<GetImageJobStatusFunction> logger)
    {
        _jobStatusService = jobStatusService;
        _logger = logger;
    }

    [Function("GetImageJobStatus")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "images/{jobId}/status")]
        HttpRequestData request,
        string jobId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(jobId, out var parsedJobId))
        {
            var badRequest = request.CreateResponse(
                HttpStatusCode.BadRequest);

            await badRequest.WriteAsJsonAsync(new
            {
                Message = "Invalid job ID."
            });

            return badRequest;
        }

        var job = await _jobStatusService.GetJobAsync(
            parsedJobId,
            cancellationToken);

        if (job is null)
        {
            var notFound = request.CreateResponse(
                HttpStatusCode.NotFound);

            await notFound.WriteAsJsonAsync(new
            {
                Message = $"Job {parsedJobId} was not found."
            });

            return notFound;
        }

        var status = job.TryGetValue("Status", out var statusValue)
            ? statusValue?.ToString()
            : "Unknown";

        var createdAt = job.TryGetValue("CreatedAt", out var createdValue)
            ? createdValue
            : null;

        var updatedAt = job.TryGetValue("UpdatedAt", out var updatedValue)
            ? updatedValue
            : null;

        var totalStations = job.TryGetValue("TotalStations", out var totalValue)
            ? totalValue
            : 0;

        var processedImages = job.TryGetValue("ProcessedImages", out var processedValue)
            ? processedValue
            : 0;

        var failedImages = job.TryGetValue("FailedImages", out var failedValue)
            ? failedValue
            : 0;

        _logger.LogInformation(
            "Retrieved status for job {JobId}: {Status}",
            parsedJobId,
            status);

        var response = request.CreateResponse(
            HttpStatusCode.OK);

        await response.WriteAsJsonAsync(new
        {
            JobId = parsedJobId,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            TotalStations = totalStations,
            ProcessedImages = processedImages,
            FailedImages = failedImages
        });

        return response;
    }
}