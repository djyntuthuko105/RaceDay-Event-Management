using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RaceDay_Event_Management.API.Data;

/// <summary>
/// Used by the EF Core tools when a migration is added. The running API does not use this class.
/// </summary>
public class RaceDayDbContextFactory : IDesignTimeDbContextFactory<RaceDayDbContext>
{
    public RaceDayDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("RaceDayDb")
            ?? throw new InvalidOperationException("Connection string RaceDayDb was not found.");

        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new RaceDayDbContext(options);
    }
}
