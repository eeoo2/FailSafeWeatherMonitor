using FailSafe_Weather_Monitor.Models;
using System.Globalization;
using System.Net.Http.Json;

namespace FailSafe_Weather_Monitor.Services;

public class WeatherApiService
{
    private const string ApiUrl =
        "https://api.open-meteo.com/v1/forecast?latitude=57.6832&longitude=11.9511&hourly=temperature_2m,relative_humidity_2m";

    // En gemensam HttpClient för hela programmet, nya för varje anrop kan ta slut på anslutningar
    private static readonly HttpClient _http = new();

    public async Task<WeatherDataPoint?> GetCurrentWeatherAsync()
    {
        var response = await _http.GetFromJsonAsync<OpenMeteoResponse>(ApiUrl);
        var hourly = response?.Hourly;

        if (hourly is null)
            return null;

        // API:et svarar i UTC, så vi letar upp timmen som pågår just nu i UTC
        var nowUtc = DateTime.UtcNow;
        string currentHour = nowUtc.ToString("yyyy-MM-dd'T'HH:00", CultureInfo.InvariantCulture);

        int index = hourly.Time.IndexOf(currentHour);

        if (index < 0 || index >= hourly.Temperature.Count || index >= hourly.Humidity.Count)
            return null;

        decimal? temperature = hourly.Temperature[index];
        decimal? humidity = hourly.Humidity[index];

        if (temperature is null || humidity is null)
            return null;

        var timestampUtc = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0, DateTimeKind.Utc);

        return new WeatherDataPoint
        {
            StationName = "Goteborg_API",
            Timestamp = timestampUtc.ToLocalTime(),
            Temperature = temperature.Value,
            Humidity = humidity.Value,
            DataSource = "API"
        };
    }
}