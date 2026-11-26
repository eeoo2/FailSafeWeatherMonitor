using FailSafe_Weather_Monitor.Data;
using FailSafe_Weather_Monitor.Services;
using FailSafe_Weather_Monitor.UI;
using Microsoft.EntityFrameworkCore;

// Skapar databasen och kör alla migrationer automatiskt vid start
try
{
    await using var db = new WeatherDbContext();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Kunde inte ansluta till databasen: {ex.Message}");
    return;
}

string filePath = Path.Combine(AppContext.BaseDirectory, "weather_data.csv");

var menu = new Menu(new FileImportService(), new WeatherApiService(), new WeatherStatsService(), filePath);
await menu.RunAsync();