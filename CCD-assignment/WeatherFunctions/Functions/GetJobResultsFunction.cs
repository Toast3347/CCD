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
        var record = await _tableClient.GetEntityAsync<JobStatusRecord>(jobId, jobId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            Status = record.Value.Status,
            Images = JsonSerializer.Deserialize<List<string>>(record.Value.ResultUrls)
        });
        return response;
    }

}