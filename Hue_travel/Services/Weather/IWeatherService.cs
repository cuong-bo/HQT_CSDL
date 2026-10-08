using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Weather
{
    /// <summary>Source of the current weather. Implementations throw <see cref="WeatherServiceException"/> when no reading is available.</summary>
    public interface IWeatherService
    {
        Task<WeatherSnapshot> GetCurrentAsync(CancellationToken cancellationToken);
    }

    /// <summary>The weather provider could not be reached or returned something unusable.</summary>
    public sealed class WeatherServiceException : Exception
    {
        public WeatherServiceException(string message) : base(message) { }

        public WeatherServiceException(string message, Exception inner) : base(message, inner) { }
    }
}
