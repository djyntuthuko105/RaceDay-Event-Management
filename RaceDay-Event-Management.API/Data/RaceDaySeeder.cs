using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Data;

public static class RaceDaySeeder
{
    public const string DemoPassword = "Password123!";

    public static async Task<bool> SeedAsync(RaceDayDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return false;

        var passwordHash = PasswordHasher.Hash(DemoPassword);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var run = new EventType { TypeName = "Run", Description = "Running events" };
        var walk = new EventType { TypeName = "Walk", Description = "Walking events" };
        var cycle = new EventType { TypeName = "Cycle", Description = "Cycling events" };
        db.EventTypes.AddRange(run, walk, cycle);

        var pretoria = new Location
        {
            VenueName = "Pretoria National Botanical Garden",
            AddressLine = "2 Cussonia Avenue",
            City = "Pretoria",
            Province = "Gauteng",
            PostalCode = "0001",
            Latitude = -25.738000m,
            Longitude = 28.267000m
        };
        var bloemfontein = new Location
        {
            VenueName = "Loch Logan Waterfront",
            AddressLine = "Waterfront Road",
            City = "Bloemfontein",
            Province = "Free State",
            PostalCode = "9301",
            Latitude = -29.118000m,
            Longitude = 26.215000m
        };
        var durban = new Location
        {
            VenueName = "Durban Beachfront",
            AddressLine = "Snell Parade",
            City = "Durban",
            Province = "KwaZulu-Natal",
            PostalCode = "4001",
            Latitude = -29.858700m,
            Longitude = 31.021800m
        };
        db.Locations.AddRange(pretoria, bloemfontein, durban);

        var thabo = User("Thabo", "Mokoena", "thabo@raceday.co.za", "0825550101", Roles.Organiser, passwordHash);
        var naledi = User("Naledi", "Dlamini", "naledi@raceday.co.za", "0835550102", Roles.Organiser, passwordHash);
        var kabelo = User("Kabelo", "Molefe", "kabelo@example.com", "0845550103", Roles.Participant, passwordHash);
        var lerato = User("Lerato", "Mokoena", "lerato@example.com", "0855550104", Roles.Participant, passwordHash);
        db.Users.AddRange(thabo, naledi, kabelo, lerato);

        var cityRun = new Event
        {
            Organiser = thabo,
            EventType = run,
            Location = pretoria,
            EventName = "Pretoria City Run",
            Description = "A community running event in Pretoria.",
            EventDate = today.AddDays(21),
            DistanceKm = 10.00m,
            RegistrationDeadline = today.AddDays(14),
            CreatedAt = DateTime.Now
        };
        var familyWalk = new Event
        {
            Organiser = naledi,
            EventType = walk,
            Location = bloemfontein,
            EventName = "Bloemfontein Family Walk",
            Description = "A family friendly walking event.",
            EventDate = today.AddDays(45),
            DistanceKm = 5.00m,
            RegistrationDeadline = today.AddDays(38),
            CreatedAt = DateTime.Now
        };
        var coastalCycle = new Event
        {
            Organiser = thabo,
            EventType = cycle,
            Location = durban,
            EventName = "Durban Coastal Cycle",
            Description = "A cycling event along the Durban coastline.",
            EventDate = today.AddDays(70),
            DistanceKm = 40.00m,
            RegistrationDeadline = today.AddDays(63),
            CreatedAt = DateTime.Now
        };
        db.Events.AddRange(cityRun, familyWalk, coastalCycle);

        var openTen = Category(cityRun, "Open 10 km", 18, 60, 10.00m, 500);
        var juniorFive = Category(cityRun, "Junior 5 km", 13, 17, 5.00m, 250);
        var familyFive = Category(familyWalk, "Family 5 km", 8, 70, 5.00m, 400);
        var seniorWalk = Category(familyWalk, "Senior Walk", 60, null, 5.00m, 150);
        var openCycle = Category(coastalCycle, "Open 40 km Cycle", 18, 65, 40.00m, 300);
        var juniorCycle = Category(coastalCycle, "Junior 20 km Cycle", 14, 17, 20.00m, 150);
        db.Categories.AddRange(openTen, juniorFive, familyFive, seniorWalk, openCycle, juniorCycle);

        db.Enrolments.AddRange(
            Enrolment(kabelo, cityRun, openTen, EnrolmentStatuses.Confirmed),
            Enrolment(lerato, cityRun, openTen, EnrolmentStatuses.Confirmed),
            Enrolment(kabelo, familyWalk, familyFive, EnrolmentStatuses.Pending),
            Enrolment(lerato, coastalCycle, openCycle, EnrolmentStatuses.Confirmed));

        db.WeatherSnapshots.AddRange(
            Weather(cityRun, 22.5m, 22.8m, "Partly Cloudy", 12.5m, 58),
            Weather(familyWalk, 19.8m, 19.2m, "Clear", 8.2m, 48),
            Weather(coastalCycle, 25.4m, 27.1m, "Sunny", 15.8m, 67));

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static User User(string firstName, string lastName, string email, string phone, string role, string passwordHash)
    {
        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phone,
            Role = role,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.Now
        };
    }

    private static Category Category(
        Event race,
        string name,
        int? minimumAge,
        int? maximumAge,
        decimal distanceKm,
        int maximumParticipants)
    {
        return new Category
        {
            Event = race,
            CategoryName = name,
            MinimumAge = minimumAge,
            MaximumAge = maximumAge,
            CategoryDistanceKm = distanceKm,
            MaximumParticipants = maximumParticipants
        };
    }

    private static Enrolment Enrolment(User participant, Event race, Category category, string status)
    {
        return new Enrolment
        {
            Participant = participant,
            Event = race,
            Category = category,
            Status = status,
            EnrolmentDate = DateTime.Now
        };
    }

    private static WeatherSnapshot Weather(
        Event race,
        decimal temperature,
        decimal feelsLike,
        string condition,
        decimal wind,
        int humidity)
    {
        return new WeatherSnapshot
        {
            Event = race,
            TemperatureCelsius = temperature,
            FeelsLikeCelsius = feelsLike,
            WeatherCondition = condition,
            WindSpeedKmh = wind,
            HumidityPercentage = humidity,
            RecordedAt = DateTime.Now
        };
    }
}
