using FailSafe_Weather_Monitor.Data;
using FailSafe_Weather_Monitor.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FailSafe_Weather_Monitor.Services;

// Resultat av importen: antal rader sparade, slängda och som var dubbletter
public record ImportResult(int Imported, int Rejected, int Duplicates);

public class FileImportService
{
    private const string BadDataLogPath = "bad_data.log";

    public async Task<ImportResult> ImportFromCsvAsync(string filePath)
    {
        var validRows = new List<WeatherDataPoint>();
        var badLines = new List<string>();
        bool isHeader = true;

        // Läser filen rad för rad, asynkront
        await foreach (var line in File.ReadLinesAsync(filePath))
        {
            // Hoppar över första raden, den innehåller bara kolumnnamnen
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            // Hoppar över tomma rader, till exempel den sista raden i filen
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Försöker tolka raden, går det inte hamnar den i felloggen
            var point = TryParseLine(line);

            if (point is null)
            {
                badLines.Add(line);
                continue;
            }

            validRows.Add(point);
        }

        // Skriver över bad_data.log så att bara felen från den senaste importen finns kvar
        await File.WriteAllLinesAsync(BadDataLogPath, badLines);

        await using var db = new WeatherDbContext();

        // Hämtar alla station + tid som redan finns, i en enda fråga istället för en per rad
        var seen = (await db.WeatherDataPoints
                .Select(w => new { w.StationName, w.Timestamp })
                .ToListAsync())
            .Select(k => (k.StationName, k.Timestamp))
            .ToHashSet();

        var toSave = new List<WeatherDataPoint>();
        int duplicates = 0;

        foreach (var point in validRows)
        {
            // Add svarar false om kombinationen redan finns, i databasen eller tidigare i filen
            if (!seen.Add((point.StationName, point.Timestamp)))
            {
                duplicates++;
                continue;
            }

            toSave.Add(point);
        }

        await db.WeatherDataPoints.AddRangeAsync(toSave);
        await db.SaveChangesAsync();

        return new ImportResult(toSave.Count, badLines.Count, duplicates);
    }

    // Gör om en textrad till en mätning, eller null om raden inte håller
    private static WeatherDataPoint? TryParseLine(string line)
    {
        var parts = line.Split('\t');

        // En giltig rad har exakt fyra kolumner
        if (parts.Length != 4)
            return null;

        string station = parts[0].Trim();

        // Stationsnamnet måste finnas och får inte vara längre än databasen tillåter
        if (string.IsNullOrWhiteSpace(station) || station.Length > 100)
            return null;

        if (!DateTime.TryParseExact(parts[1].Trim(), "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
            return null;

        // Byter kommatecken mot punkt så att svenska decimaltal också går igenom
        string tempText = parts[2].Trim().Replace(',', '.');
        string humidityText = parts[3].Trim().Replace(',', '.');

        if (!decimal.TryParse(tempText, NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature))
            return null;

        if (!decimal.TryParse(humidityText, NumberStyles.Float, CultureInfo.InvariantCulture, out var humidity))
            return null;

        // Rimlighetskontroll, till exempel 999.9 är ett sensorfel och inte en riktig temperatur
        if (temperature < -60 || temperature > 60)
            return null;

        if (humidity < 0 || humidity > 100)
            return null;

        return new WeatherDataPoint
        {
            StationName = station,
            Timestamp = timestamp,
            Temperature = temperature,
            Humidity = humidity,
            DataSource = "CSV-File"
        };
    }
}