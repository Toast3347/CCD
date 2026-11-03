using System;
using System.Collections.Generic;
using System.Text;

namespace ExternalAPI.Models
{
    public class WeatherStation
    {
        public int? StationId { get; set; }

        public string StationName { get; set; } = string.Empty;

        public string? Region { get; set; }

        public double? Temperature { get; set; }

        public double? FeelTemperature { get; set; }

        public double? Humidity { get; set; }

        public double? WindSpeed { get; set; }

        public int? WindSpeedBft { get; set; }

        public string? WindDirection { get; set; }

        public string? WeatherDescription { get; set; }

        public double? Precipitation { get; set; }

        public double? AirPressure { get; set; }

        public string? IconUrl { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }
}
