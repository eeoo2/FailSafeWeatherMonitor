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