using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace WeatherFunctions.Services
{
    public sealed class QueueStorageService
    {
        private readonly QueueClient _weatherJobsQueue;
        private readonly QueueClient _imageJobsQueue;

        public QueueStorageService(string connectionString)
        {
            var options = new QueueClientOptions(
                QueueClientOptions.ServiceVersion.V2026_06_06)
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            _weatherJobsQueue = new QueueClient(
                connectionString,
                QueueNames.WeatherJobs,
                options);

            _imageJobsQueue = new QueueClient(
                connectionString,
                QueueNames.ImageJobs,
                options);
        }

        public async Task InitializeAsync(
            CancellationToken cancellationToken = default)
        {
            await _weatherJobsQueue.CreateIfNotExistsAsync(
                cancellationToken: cancellationToken);

            await _imageJobsQueue.CreateIfNotExistsAsync(
                cancellationToken: cancellationToken);
        }

        public async Task SendWeatherJobAsync<T>(
            T message,
            CancellationToken cancellationToken = default)
        {
            await _weatherJobsQueue.CreateIfNotExistsAsync(
                cancellationToken: cancellationToken);

            var json = JsonSerializer.Serialize(message);

            await _weatherJobsQueue.SendMessageAsync(
                json,
                cancellationToken);
        }

        public async Task SendImageJobAsync<T>(
            T message,
            CancellationToken cancellationToken = default)
        {
            await _imageJobsQueue.CreateIfNotExistsAsync(
                cancellationToken: cancellationToken);

            var json = JsonSerializer.Serialize(message);

            await _imageJobsQueue.SendMessageAsync(
                json,
                cancellationToken);
        }

    }
}
