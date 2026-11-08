using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RaceDay_Event_Management.API.Data;

namespace RaceDay.Web.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            var existing = services.Where(service =>
                service.ServiceType == typeof(DbContextOptions<RaceDayDbContext>) ||
                service.ServiceType == typeof(RaceDayDbContext)).ToList();

            foreach (var descriptor in existing)
                services.Remove(descriptor);

            services.AddDbContext<RaceDayDbContext>(options => options.UseSqlite(_connection));
            services.AddHttpClient<RaceDay_Event_Management.API.Services.CurrentConditionsService>().ConfigurePrimaryHttpMessageHandler(() => new WeatherStub());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        db.Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

public class WeatherStub : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"current\":{\"temperature_2m\":21,\"weather_code\":0,\"wind_speed_10m\":10,\"time\":1794175200}}") });
}
