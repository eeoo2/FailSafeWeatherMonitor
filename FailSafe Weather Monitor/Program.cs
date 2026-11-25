using FailSafe_Weather_Monitor.Services;
using FailSafe_Weather_Monitor.Data;
using Microsoft.EntityFrameworkCore;

// Skapar databasen och kör alla migrationer automatiskt vid start
await using (var db = new WeatherDbContext())
{
    await db.Database.MigrateAsync();
}

var importer = new FileImportService();
string filePath = Path.Combine(AppContext.BaseDirectory, "weather_data.csv");

try
{
    var result = await importer.ImportFromCsvAsync(filePath);

    Console.WriteLine($"{result.Imported} rader importerade, {result.Rejected} felaktiga rader hoppades över.");
    Console.WriteLine($"{result.Duplicates} dubbletter hoppades över.");
}
catch (FileNotFoundException)
{
    Console.WriteLine("Hittade inte datafilen weather_data.csv.");
}
catch (Exception ex)
{
    // Fångar allt annat, till exempel om databasen inte går att nå
    Console.WriteLine($"Importen misslyckades: {ex.Message}");
}

var api = new WeatherApiService();

try
{
    var apiResult = await api.FetchAndSaveAsync();
    var p = apiResult.Point;

    if (p is null)
        Console.WriteLine("Hittade ingen mätning för den aktuella timmen.");
    else if (apiResult.Saved)
        Console.WriteLine($"Sparade: {p.StationName} {p.Timestamp:yyyy-MM-dd HH:mm}, {p.Temperature} °C, {p.Humidity} %");
    else
        Console.WriteLine("Mätningen för den här timmen finns redan.");
}
catch (HttpRequestException)
{
    Console.WriteLine("Kunde inte nå väder-API:et. Kontrollera internetanslutningen.");
}
catch (Exception ex)
{
    Console.WriteLine($"API-anropet misslyckades: {ex.Message}");
}