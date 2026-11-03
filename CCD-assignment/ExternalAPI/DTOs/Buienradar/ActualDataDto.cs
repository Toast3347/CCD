using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ExternalAPI.DTOs.Buienradar
{
    public sealed class ActualDataDto
    {
        [JsonPropertyName("stationmeasurements")]
        public List<StationDataDto> StationMeasurements { get; set; } = [];
    }
}
