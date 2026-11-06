using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherFunctions.Models;

namespace WeatherFunctions.Functions;

public class GetJobResultsFunction
{
    private readonly ILogger<GetJobResultsFunction> _logger;
    private readonly TableClient _tableClient;

    public GetJobResultsFunction(ILogger<GetJobResultsFunction> logger, TableClient tableClient)
    {
        _logger = logger;
        _tableClient = tableClient;
    }

    [Function("GetJobResults")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "images/{jobId}")] HttpRequestData req,
        string jobId)
    {
        if (!Guid.TryParse(jobId, out var parsedJobId))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { Message = "Invalid job ID." });
            return badRequest;
        }

        var record = await _tableClient.GetEntityAsync<JobStatusRecord>("ImageJobs", jobId);

        if (record is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { Message = $"Job {jobId} was not found." });
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);

        var urls = JsonSerializer.Deserialize<List<string>>(record.Value.ResultUrls) ?? new List<string>();

        await response.WriteAsJsonAsync(new
        {
            Status = record.Value.Status,
            Images = urls
        });

        return response;
    }
}