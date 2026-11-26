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
    private readonly string filePath;

    public Menu(FileImportService importer, WeatherApiService api, string filePath)
    {
        this.importer = importer;
        this.api = api;
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
                ShowStatistics();
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

    private static void ShowStatistics()
    {
        // TODO: Visa stationer och statistik
        ShowError("Statistiken är inte tillgänglig än.");
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