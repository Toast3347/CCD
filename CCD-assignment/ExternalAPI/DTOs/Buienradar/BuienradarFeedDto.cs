using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ExternalAPI.DTOs.Buienradar
{
    public sealed class BuienradarFeedDto
    {
        [JsonPropertyName("actual")]
        public ActualDataDto? Actual { get; set; }
    }
}
