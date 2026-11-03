using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ExternalAPI.DTOs.Buienradar
{
    public sealed class StationDataDto
    {
        [JsonPropertyName("stationid")]
        public int? StationId { get; set; }

        [JsonPropertyName("stationname")]
        public string? StationName { get; set; }

        [JsonPropertyName("regio")]
        public string? Region { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("feeltemperature")]
        public double? FeelTemperature { get; set; }

        [JsonPropertyName("humidity")]
        public double? Humidity { get; set; }

        [JsonPropertyName("windspeed")]
        public double? WindSpeed { get; set; }

        [JsonPropertyName("windspeedBft")]
        public int? WindSpeedBft { get; set; }

        [JsonPropertyName("winddirection")]
        public string? WindDirection { get; set; }

        [JsonPropertyName("weatherdescription")]
        public string? WeatherDescription { get; set; }

        [JsonPropertyName("precipitation")]
        public double? Precipitation { get; set; }

        [JsonPropertyName("airpressure")]
        public double? AirPressure { get; set; }

        [JsonPropertyName("iconurl")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
    }
}
