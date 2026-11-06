using Azure;
using Azure.Data.Tables;
using System.Text.Json;
using WeatherFunctions.Models;

namespace WeatherFunctions.Services;

public sealed class JobStatusService
{
    private readonly TableClient _tableClient;

    public JobStatusService(TableClient tableClient)
    {
        _tableClient = tableClient;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await _tableClient.CreateIfNotExistsAsync(
            cancellationToken);
    }

    public async Task CreateJobAsync(
        Guid jobId,
        int totalcount,
        CancellationToken cancellationToken = default)
    {
        var entity = new JobStatusRecord
        {
            PartitionKey = "ImageJobs",
            RowKey = jobId.ToString(),
            Status = "Queued",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            TotalStations = totalcount,
            ProcessedImages = 0,
            FailedImages = 0,
            ResultUrls = "[]"
        };

        await _tableClient.AddEntityAsync(
            entity,
            cancellationToken);
    }

    public async Task<JobStatusRecord?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var response =
            await _tableClient.GetEntityIfExistsAsync<JobStatusRecord>(
                "ImageJobs",
                jobId.ToString(),
                cancellationToken: cancellationToken);

        if (response.HasValue)
        {
            return response.Value;
        }

        return null;
    }

    public async Task UpdateProgressAsync(
        JobStatusRecord entity,
        CancellationToken cancellationToken = default)
    {
        int total = entity.TotalStations > 0 ? entity.TotalStations : 50;

        if (entity.FailedImages > 0)
        {
            entity.Status = "PartiallyFailed";
        }
        else if (entity.ProcessedImages >= total)
        {
            entity.Status = "Completed";
        }
        else if (entity.ProcessedImages > 0)
        {
            entity.Status = "In Progress";
        }
        else
        {
            entity.Status = "Queued";
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _tableClient.UpdateEntityAsync(
            entity,
            entity.ETag,
            TableUpdateMode.Merge,
            cancellationToken);
    }

    public async Task UpdateStatusAsync(
        Guid jobId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetJobAsync(jobId, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException(
                $"Job {jobId} was not found.");
        }

        entity.Status = status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _tableClient.UpdateEntityAsync(
            entity,
            entity.ETag,
            TableUpdateMode.Replace,
            cancellationToken);
    }
}