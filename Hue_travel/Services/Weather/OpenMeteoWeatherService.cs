using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Weather
{
    /// <summary>
    /// Current weather from the free Open-Meteo API (https://open-meteo.com, no key needed; the data is CC BY 4.0
    /// and free for non-commercial use, so keep the attribution).
    /// </summary>
    public sealed class OpenMeteoWeatherService : IWeatherService
    {
        public const double HueLatitude = 16.4637;
        public const double HueLongitude = 107.5909;

        private static readonly HttpClient SharedClient = CreateClient();

        private readonly HttpClient client;
        private readonly Uri requestUri;

        public OpenMeteoWeatherService(double latitude = HueLatitude, double longitude = HueLongitude, HttpClient httpClient = null)
        {
            client = httpClient ?? SharedClient;
            requestUri = new Uri(string.Format(CultureInfo.InvariantCulture,
                "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=temperature_2m,weather_code,is_day&timezone=Asia%2FBangkok",
                latitude, longitude));
        }

        public async Task<WeatherSnapshot> GetCurrentAsync(CancellationToken cancellationToken)
        {
            string body;
            try
            {
                using (HttpResponseMessage response = await client.GetAsync(requestUri, cancellationToken).ConfigureAwait(true))
                {
                    response.EnsureSuccessStatusCode();
                    body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new WeatherServiceException("Open-Meteo request failed: " + ex.Message, ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new WeatherServiceException("Open-Meteo request timed out.", ex);
            }

            WeatherSnapshot snapshot;
            if (!OpenMeteoParser.TryParse(body, out snapshot))
                throw new WeatherServiceException("Open-Meteo returned an unexpected response.");
            return snapshot;
        }

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            return new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }
    }
}
