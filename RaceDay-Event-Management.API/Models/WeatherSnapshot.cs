namespace RaceDay_Event_Management.API.Models;

public class WeatherSnapshot
{
    public int WeatherSnapshotId { get; set; }

    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public decimal? TemperatureCelsius { get; set; }

    public decimal? FeelsLikeCelsius { get; set; }

    public string? WeatherCondition { get; set; }

    public decimal? WindSpeedKmh { get; set; }

    public int? HumidityPercentage { get; set; }

    public DateTime RecordedAt { get; set; }
}
