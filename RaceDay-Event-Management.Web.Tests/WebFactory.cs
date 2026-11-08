extern alias web;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Services;
namespace RaceDay.Web.Tests;
public class WebFactory : WebApplicationFactory<web::Program>
{
    public ApiFactory Api { get; } = new();
    public WebFactory()
    {
        using var scope = Api.Services.CreateScope();
        RaceDaySeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RaceDayDbContext>()).GetAwaiter().GetResult();
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<web::RaceDay.Web.Services.RaceDayApi>(client => client.BaseAddress = new Uri("http://localhost/"))
                .ConfigurePrimaryHttpMessageHandler(() => Api.Server.CreateHandler());
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) Api.Dispose();
    }
}
