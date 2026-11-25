using System.Text.Json.Serialization;

namespace FailSafe_Weather_Monitor.Models;

// Matchar JSON-svaret från Open-Meteo, bara de delar vi behöver
public class OpenMeteoResponse
{
    [JsonPropertyName("hourly")]
    public HourlyData? Hourly { get; set; }
}

public class HourlyData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperature_2m")]
    public List<decimal?> Temperature { get; set; } = [];

    [JsonPropertyName("relative_humidity_2m")]
    public List<decimal?> Humidity { get; set; } = [];
}