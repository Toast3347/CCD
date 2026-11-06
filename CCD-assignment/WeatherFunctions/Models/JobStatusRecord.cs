using Azure;
using Azure.Data.Tables;

namespace WeatherFunctions.Models
{
    public class JobStatusRecord : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Status { get; set; } = "Queued";
        public int TotalStations { get; set; }
        public int ProcessedImages { get; set; }
        public int FailedImages { get; set; }
        public string ResultUrls { get; set; } = "[]";
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}