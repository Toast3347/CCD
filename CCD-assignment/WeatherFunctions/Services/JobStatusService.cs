using Azure;
using Azure.Data.Tables;

namespace WeatherFunctions.Services;

public sealed class JobStatusService
{
    private readonly TableClient _tableClient;

    public JobStatusService(string connectionString)
    {
        _tableClient = new TableClient(
            connectionString,
            "ImageJobs");
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await _tableClient.CreateIfNotExistsAsync(
            cancellationToken);
    }

    public async Task CreateJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var entity = new TableEntity(
            "ImageJobs",
            jobId.ToString())
        {
            ["Status"] = "Queued",
            ["CreatedAt"] = DateTimeOffset.UtcNow,
            ["UpdatedAt"] = DateTimeOffset.UtcNow,
            ["TotalStations"] = 0,
            ["ProcessedImages"] = 0,
            ["FailedImages"] = 0
        };

        await _tableClient.AddEntityAsync(
            entity,
            cancellationToken);
    }

    public async Task<TableEntity?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var response =
            await _tableClient.GetEntityIfExistsAsync<TableEntity>(
                "ImageJobs",
                jobId.ToString(),
                cancellationToken: cancellationToken);

        return response.HasValue ? response.Value : null;
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

        entity["Status"] = status;
        entity["UpdatedAt"] = DateTimeOffset.UtcNow;

        await _tableClient.UpsertEntityAsync(
            entity,
            TableUpdateMode.Merge,
            cancellationToken);
    }
}