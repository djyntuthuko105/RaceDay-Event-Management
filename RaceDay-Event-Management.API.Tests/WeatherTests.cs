using System.Net;
using Microsoft.Extensions.Caching.Memory;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Services;
using Xunit;
namespace RaceDay_Event_Management.API.Tests;
public class WeatherTests
{
    private static Location Venue() => new() { LocationId = 1, VenueName = "Test venue", City = "Pretoria", Latitude = -25.7m, Longitude = 28.2m };
    [Fact]
    public async Task Conditions_ReturnCurrentValuesAndSouthAfricanObservationTime()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var http = new HttpClient(new Stub(HttpStatusCode.OK, "{\"current\":{\"temperature_2m\":20,\"wind_speed_10m\":12,\"weather_code\":2,\"time\":1794175200}}")) { BaseAddress = new Uri("https://example.test/") };
        var result = await new CurrentConditionsService(http, cache).GetAsync(Venue(), default);
        Assert.Equal(20m, result.TemperatureCelsius);
        Assert.Equal("Partly cloudy", result.Condition);
        Assert.Equal(TimeSpan.FromHours(2), result.ObservedAt!.Value.Offset);
        Assert.Contains("not a forecast", result.Notice);
        Assert.StartsWith("https://www.google.com/maps/dir/", result.DirectionsUrl);
    }
    [Fact]
    public async Task Conditions_WhenProviderFails_ReturnHelpfulFallbackNotFakeWeather()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var http = new HttpClient(new Stub(HttpStatusCode.ServiceUnavailable, "unavailable")) { BaseAddress = new Uri("https://example.test/") };
        var result = await new CurrentConditionsService(http, cache).GetAsync(Venue(), default);
        Assert.Null(result.TemperatureCelsius); Assert.Null(result.ObservedAt);
        Assert.Contains("unavailable", result.Notice);
        Assert.NotEmpty(result.DirectionsUrl);
    }
    [Fact]
    public async Task Conditions_WithoutCoordinates_ReturnVenueDirectionsAndExplainMissingWeather()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var http = new HttpClient(new Stub(HttpStatusCode.OK, "{}"));
        var result = await new CurrentConditionsService(http, cache).GetAsync(new Location { VenueName = "City Park", City = "Pretoria" }, default);
        Assert.Contains("coordinates", result.Notice);
        Assert.Contains("City%20Park", result.DirectionsUrl);
        Assert.Null(result.ObservedAt);
    }
    private class Stub(HttpStatusCode status, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) });
    }
}
