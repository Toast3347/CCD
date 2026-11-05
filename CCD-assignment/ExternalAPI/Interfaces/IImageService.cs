using ExternalAPI.Models;

namespace ExternalAPI.Interfaces
{
    public interface IImageService
    {
        Task<ImageProcessingResult> GetAndProcessImageAsync(WeatherStation station, Guid jobId);
    }
}
