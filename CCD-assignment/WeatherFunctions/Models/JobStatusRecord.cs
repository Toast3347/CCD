using Azure;
using Azure.Data.Tables;

namespace WeatherFunctions.Models
{
    public class JobStatusRecord : ITableEntity
    {
        public string PartitionKey { get; set; } = "JobData"; 
        public string RowKey { get; set; }
        public string Status { get; set; } = "Queued";
        public int TotalImages { get; set; } = 50;
        public int CompletedImages { get; set; } = 0;
        public string ResultUrls { get; set; } = "[]"; 
        public DateTimeOffset? Timestamp { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ETag ETag { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
