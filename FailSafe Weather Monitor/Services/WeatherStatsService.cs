using FailSafe_Weather_Monitor.Data;
using FailSafe_Weather_Monitor.Models;
using Microsoft.EntityFrameworkCore;

namespace FailSafe_Weather_Monitor.Services;

// Högsta, lägsta och genomsnittlig temperatur för en station
public record StationStats(decimal Max, decimal Min, decimal Average);

public class WeatherStatsService
{
    public async Task<List<string>> GetStationNamesAsync()
    {
        await using var db = new WeatherDbContext();

        return await db.WeatherDataPoints
            .Select(w => w.StationName)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();
    }

    public async Task<List<WeatherDataPoint>> GetMeasurementsAsync(string stationName)
    {
        await using var db = new WeatherDbContext();

        return await db.WeatherDataPoints
            .AsNoTracking()
            .Where(w => w.StationName == stationName)
            .OrderBy(w => w.Timestamp)
            .ToListAsync();
    }

    public async Task<StationStats> GetStatsAsync(string stationName)
    {
        await using var db = new WeatherDbContext();

        var temperatures = db.WeatherDataPoints
            .Where(w => w.StationName == stationName)
            .Select(w => w.Temperature);

        return new StationStats(
            await temperatures.MaxAsync(),
            await temperatures.MinAsync(),
            await temperatures.AverageAsync());
    }
}