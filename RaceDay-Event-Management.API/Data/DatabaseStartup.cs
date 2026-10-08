using Microsoft.EntityFrameworkCore;

namespace RaceDay_Event_Management.API.Data;

public static class DatabaseStartup
{
    public static async Task ApplyAsync(WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RaceDay");

        try
        {
            await db.Database.MigrateAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                "Could not open RaceDayDB. Check that SQL Server is running and that the RaceDayDb connection string in appsettings.json matches the server name you use in SSMS.");
            throw;
        }

        if (await RaceDaySeeder.SeedAsync(db))
            logger.LogInformation("Demo accounts were created. The sample password is listed in the README.");
    }
}
