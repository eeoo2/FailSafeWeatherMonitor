using FailSafe_Weather_Monitor.Models;
using FailSafe_Weather_Monitor.Services;
using Spectre.Console;

namespace FailSafe_Weather_Monitor.UI;

public class Menu
{
    private const string ImportChoice = "Importera historisk data från fil";
    private const string ApiChoice = "Hämta live-data från API";
    private const string StatsChoice = "Visa väderstatistik";
    private const string ExitChoice = "Avsluta";

    private readonly FileImportService importer;
    private readonly WeatherApiService api;
    private readonly WeatherStatsService stats;
    private readonly string filePath;

    public Menu(FileImportService importer, WeatherApiService api, WeatherStatsService stats, string filePath)
    {
        this.importer = importer;
        this.api = api;
        this.stats = stats;
        this.filePath = filePath;
    }

    public async Task RunAsync()
    {
        bool running = true;
        while (running)
        {
            string choice = ShowMainMenu();
            running = await HandleChoiceAsync(choice);
        }
    }

    private static string ShowMainMenu()
    {
        AnsiConsole.Clear();
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]FailSafe WeatherMonitor[/] – välj ett alternativ")
                .AddChoices(ImportChoice, ApiChoice, StatsChoice, ExitChoice));
    }

    private async Task<bool> HandleChoiceAsync(string choice)
    {
        switch (choice)
        {
            case ImportChoice:
                await ImportFromFileAsync();
                break;
            case ApiChoice:
                await FetchFromApiAsync();
                break;
            case StatsChoice:
                await ShowStatisticsAsync();
                break;
            case ExitChoice:
                return false;
        }

        WaitForKey();
        return true;
    }

    private async Task ImportFromFileAsync()
    {
        try
        {
            var result = await importer.ImportFromCsvAsync(filePath);
            ShowSuccess($"{result.Imported} rader importerade, {result.Rejected} felaktiga rader hoppades över.");
            AnsiConsole.MarkupLine($"[grey]{result.Duplicates} dubbletter hoppades över.[/]");
        }
        catch (FileNotFoundException)
        {
            ShowError("Hittade inte datafilen weather_data.csv.");
        }
        catch (Exception ex)
        {
            ShowError($"Importen misslyckades: {ex.Message}");
        }
    }

    private async Task FetchFromApiAsync()
    {
        try
        {
            var apiResult = await api.FetchAndSaveAsync();
            var p = apiResult.Point;

            if (p is null)
                ShowError("Hittade ingen mätning för den aktuella timmen.");
            else if (apiResult.Saved)
                ShowSuccess($"Sparade: {p.StationName} {p.Timestamp:yyyy-MM-dd HH:mm}, {p.Temperature} °C, {p.Humidity} %");
            else
                AnsiConsole.MarkupLine("[yellow]Mätningen för den här timmen finns redan.[/]");
        }
        catch (HttpRequestException)
        {
            ShowError("Kunde inte nå väder-API:et. Kontrollera internetanslutningen.");
        }
        catch (Exception ex)
        {
            ShowError($"API-anropet misslyckades: {ex.Message}");
        }
    }

    private async Task ShowStatisticsAsync()
    {
        try
        {
            var stations = await stats.GetStationNamesAsync();

            if (stations.Count == 0)
            {
                ShowError("Det finns ingen data än. Importera från fil eller hämta från API först.");
                return;
            }

            ShowStationsTable(stations);

            string station = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Välj en mätstation")
                    .AddChoices(stations));

            var measurements = await stats.GetMeasurementsAsync(station);
            var stationStats = await stats.GetStatsAsync(station);

            AnsiConsole.Clear();
            ShowMeasurementsTable(station, measurements);
            ShowStatsTable(stationStats);
        }
        catch (Exception ex)
        {
            ShowError($"Kunde inte hämta statistiken: {ex.Message}");
        }
    }

    private static void ShowStationsTable(List<string> stations)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey);
        table.AddColumn("[bold]Mätstationer[/]");

        foreach (var name in stations)
            table.AddRow(Markup.Escape(name));

        AnsiConsole.Write(table);
    }

    private static void ShowMeasurementsTable(string station, List<WeatherDataPoint> measurements)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey)
            .Title($"[bold]{Markup.Escape(station)}[/] – {measurements.Count} mätningar");

        table.AddColumn("[bold]Tid[/]");
        table.AddColumn(new TableColumn("[bold]Temperatur (°C)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Luftfuktighet (%)[/]").RightAligned());
        table.AddColumn("[bold]Källa[/]");

        foreach (var m in measurements)
        {
            table.AddRow(
                m.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                m.Temperature.ToString("0.0##"),
                m.Humidity.ToString("0.0"),
                m.DataSource);
        }

        AnsiConsole.Write(table);
    }

    private static void ShowStatsTable(StationStats s)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey)
            .Title("[bold]Temperaturstatistik[/]");

        table.AddColumn(new TableColumn("[bold red]Högsta[/]").Centered());
        table.AddColumn(new TableColumn("[bold blue]Lägsta[/]").Centered());
        table.AddColumn(new TableColumn("[bold yellow]Medel[/]").Centered());

        table.AddRow(
            $"[red]{s.Max:0.0##} °C[/]",
            $"[blue]{s.Min:0.0##} °C[/]",
            $"[yellow]{s.Average:0.00} °C[/]");

        AnsiConsole.Write(table);
    }

    private static void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");
    }

    private static void ShowError(string message)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
    }

    private static void WaitForKey()
    {
        AnsiConsole.MarkupLine("[grey]Tryck på valfri tangent för att fortsätta...[/]");
        Console.ReadKey(true);
    }
}