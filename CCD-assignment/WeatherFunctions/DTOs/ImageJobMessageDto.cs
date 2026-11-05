using ExternalAPI.Models;

namespace WeatherFunctions.DTOs
{
    public sealed class ImageJobMessageDto
    {
        public Guid JobId { get; set; }

        public WeatherStation WeatherStation { get; set; } = new();
    }
}
