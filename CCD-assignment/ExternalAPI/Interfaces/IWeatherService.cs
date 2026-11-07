using ExternalAPI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExternalAPI.Interfaces
{
    public interface IWeatherService
    {
        Task<List<WeatherStation>> GetStationsAsync( int count,
            CancellationToken cancellationToken = default);
    }
}
