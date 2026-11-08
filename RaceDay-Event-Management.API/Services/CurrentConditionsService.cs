using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Models;
namespace RaceDay_Event_Management.API.Services;
public class CurrentConditionsService(HttpClient client, IMemoryCache cache)
{
    public async Task<RaceConditionsResponse> GetAsync(Location venue, CancellationToken cancellationToken)
    {
        var destination = venue.Latitude is not null && venue.Longitude is not null
            ? FormattableString.Invariant($"{venue.Latitude},{venue.Longitude}") : $"{venue.VenueName}, {venue.AddressLine}, {venue.City}, South Africa";
        var key = $"conditions:{venue.LocationId}:{destination}";
        if (cache.TryGetValue<RaceConditionsResponse>(key, out var cached) && cached is not null) return cached;
        var result = new RaceConditionsResponse { DirectionsUrl = "https://www.google.com/maps/dir/?api=1&destination=" + Uri.EscapeDataString(destination) };
        if (venue.Latitude is null || venue.Longitude is null)
        { result.Notice = "Weather needs venue coordinates. Ask the organiser for the latest race-day guidance."; return result; }
        try
        {
            var path = FormattableString.Invariant($"v1/forecast?latitude={venue.Latitude}&longitude={venue.Longitude}&current=temperature_2m,weather_code,wind_speed_10m&timezone=Africa%2FJohannesburg&timeformat=unixtime");
            using var response = await client.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var current = json.RootElement.GetProperty("current");
            result.TemperatureCelsius = current.GetProperty("temperature_2m").GetDecimal();
            result.WindSpeedKmh = current.GetProperty("wind_speed_10m").GetDecimal();
            result.ObservedAt = DateTimeOffset.FromUnixTimeSeconds(current.GetProperty("time").GetInt64()).ToOffset(TimeSpan.FromHours(2));
            result.Condition = current.GetProperty("weather_code").GetInt32() switch
            {
                0 => "Clear",
                1 or 2 => "Partly cloudy",
                3 => "Overcast",
                45 or 48 => "Fog",
                >= 51 and <= 67 => "Rain or drizzle",
                >= 71 and <= 77 => "Snow",
                >= 80 and <= 86 => "Showers",
                >= 95 => "Thunderstorms",
                _ => "Variable conditions"
            };
            result.Notice = "Current conditions, not a forecast for the event date. Data: Open-Meteo (CC BY 4.0).";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            if (cancellationToken.IsCancellationRequested) throw;
        }
        cache.Set(key, result, TimeSpan.FromMinutes(result.ObservedAt is null ? 1 : 10));
        return result;
    }
}
