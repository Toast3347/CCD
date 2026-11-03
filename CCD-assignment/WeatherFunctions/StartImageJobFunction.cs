using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WeatherFunctions.DTOs;

namespace WeatherFunctions;

public class StartImageJobFunction
{
    private readonly QueueClient _queueClient;

    public StartImageJobFunction(QueueClient queueClient)
    {
        _queueClient = queueClient;
    }

    [Function("StartImageJob")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous,"post",Route = "images")]HttpRequestData request)
    {
        var jobId = Guid.NewGuid();

        var message = new WeatherJobMessageDto
        {
            JobId = jobId
        };

        await _queueClient.CreateIfNotExistsAsync();

        var serializedMessage = JsonSerializer.Serialize(message);

        await _queueClient.SendMessageAsync(serializedMessage);

        var baseUrl = request.Url.GetLeftPart(UriPartial.Authority);

        var response = request.CreateResponse(HttpStatusCode.Accepted);

        await response.WriteAsJsonAsync(new
        {
            JobId = jobId,
            Status = "Queued",
            StatusUrl = $"{baseUrl}/api/images/{jobId}/status",
            ResultsUrl = $"{baseUrl}/api/images/{jobId}"
        });

        return response;
    }
}