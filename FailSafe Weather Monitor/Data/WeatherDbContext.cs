using FailSafe_Weather_Monitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FailSafe_Weather_Monitor.Data;

public class WeatherDbContext : DbContext
{
    public DbSet<WeatherDataPoint> WeatherDataPoints { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

            // Läs anslutningssträngen från appsettings.json (inte hårdkodad!)
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                // Läs även appsettings.Production.json när appen körs mot Azure
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Dubblettskydd: samma station + samma tidpunkt får bara finnas en gång
        modelBuilder.Entity<WeatherDataPoint>()
            .HasIndex(w => new { w.StationName, w.Timestamp })
            .IsUnique();

        // Explicit precision på decimaltalen
        modelBuilder.Entity<WeatherDataPoint>()
            .Property(w => w.Temperature)
            .HasPrecision(6, 3);

        modelBuilder.Entity<WeatherDataPoint>()
            .Property(w => w.Humidity)
            .HasPrecision(5, 2);
    }
}