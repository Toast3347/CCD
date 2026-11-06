using ExternalAPI.Interfaces;
using ExternalAPI.Models;
using System.Text.Json;
using ExternalAPI.DTOs.Buienradar;

namespace ExternalAPI.Services
{
    public class BuienradarService :IWeatherService
    {
        private const string ApiUrl = "https://data.buienradar.nl/2.0/feed/json";

        private readonly HttpClient _httpClient;

        public BuienradarService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<WeatherStation>> GetStationsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(
            ApiUrl, cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken);

            var feed = await JsonSerializer.DeserializeAsync<BuienradarFeedDto>(
                stream,
                cancellationToken: cancellationToken);

            if (feed?.Actual?.StationMeasurements == null)
            {
                throw new InvalidOperationException(
                    "Buienradar returned no weather stations.");
            }

            return feed.Actual.StationMeasurements
                .Take(50)
                .Select(station => new WeatherStation
                {
                    StationId = station.StationId,
                    StationName = station.StationName ?? "Unknown",
                    Region = station.Region,
                    Temperature = station.Temperature,
                    FeelTemperature = station.FeelTemperature,
                    Humidity = station.Humidity,
                    WindSpeed = station.WindSpeed,
                    WindSpeedBft = station.WindSpeedBft,
                    WindDirection = station.WindDirection,
                    WeatherDescription = station.WeatherDescription,
                    Precipitation = station.Precipitation,
                    AirPressure = station.AirPressure,
                    IconUrl = station.IconUrl,
                    Timestamp = DateTimeOffset.TryParse(
                        station.Timestamp, out var timestamp)
                        ? timestamp
                        : null
                })
                .ToList();
        }
    }
}

