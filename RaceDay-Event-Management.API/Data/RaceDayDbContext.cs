using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Models;

namespace RaceDay_Event_Management.API.Data;

public class RaceDayDbContext : DbContext
{
    private readonly bool _sqlServer;

    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
        // Tests use SQLite, which does not understand SQL Server functions such as GETDATE().
        _sqlServer = options.Extensions.Any(extension =>
            extension.GetType().Name.Contains("SqlServer", StringComparison.Ordinal));
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<EventType> EventTypes => Set<EventType>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    public DbSet<Result> Results => Set<Result>();

    public DbSet<EventImage> EventImages => Set<EventImage>();

    public DbSet<WeatherSnapshot> WeatherSnapshots => Set<WeatherSnapshot>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // The Part 1 script uses VARCHAR. EF would otherwise create NVARCHAR columns.
        configurationBuilder.Properties<string>().AreUnicode(false);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureEventTypes(modelBuilder);
        ConfigureLocations(modelBuilder);
        ConfigureEvents(modelBuilder);
        ConfigureCategories(modelBuilder);
        ConfigureEnrolments(modelBuilder);
        ConfigureResults(modelBuilder);
        ConfigureEventImages(modelBuilder);
        ConfigureWeather(modelBuilder);
    }

    private void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();

        entity.ToTable("Users", table =>
            table.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));

        entity.HasKey(user => user.UserId);
        entity.Property(user => user.FirstName).HasMaxLength(50).IsRequired();
        entity.Property(user => user.LastName).HasMaxLength(50).IsRequired();
        entity.Property(user => user.Email).HasMaxLength(100).IsRequired();
        entity.HasIndex(user => user.Email).IsUnique();
        entity.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
        entity.Property(user => user.PhoneNumber).HasMaxLength(20);
        entity.Property(user => user.Role).HasMaxLength(20).IsRequired();
        entity.Property(user => user.ProfilePictureUrl).HasMaxLength(500);
        ApplyTimestampDefault(entity.Property(user => user.CreatedAt));
    }

    private void ConfigureEventTypes(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EventType>();

        entity.ToTable("EventTypes");
        entity.HasKey(type => type.EventTypeId);
        entity.Property(type => type.TypeName).HasMaxLength(30).IsRequired();
        entity.HasIndex(type => type.TypeName).IsUnique();
        entity.Property(type => type.Description).HasMaxLength(255);
    }

    private void ConfigureLocations(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Location>();

        entity.ToTable("Locations");
        entity.HasKey(location => location.LocationId);
        entity.Property(location => location.VenueName).HasMaxLength(150).IsRequired();
        entity.Property(location => location.AddressLine).HasMaxLength(255).IsRequired();
        entity.Property(location => location.City).HasMaxLength(100).IsRequired();
        entity.Property(location => location.Province).HasMaxLength(100).IsRequired();
        entity.Property(location => location.PostalCode).HasMaxLength(10);
        entity.Property(location => location.Latitude).HasPrecision(9, 6);
        entity.Property(location => location.Longitude).HasPrecision(9, 6);
    }

    private void ConfigureEvents(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Event>();

        entity.ToTable("Events", table =>
            table.HasCheckConstraint("CK_Events_Distance", "[DistanceKm] > 0"));

        entity.HasKey(race => race.EventId);
        entity.Property(race => race.EventName).HasMaxLength(150).IsRequired();
        entity.Property(race => race.Description).HasMaxLength(1000).IsRequired();
        entity.Property(race => race.EventDate).HasColumnType("date");
        entity.Property(race => race.DistanceKm).HasPrecision(6, 2);
        entity.Property(race => race.RegistrationDeadline).HasColumnType("date");
        ApplyTimestampDefault(entity.Property(race => race.CreatedAt));

        entity.HasOne(race => race.Organiser)
            .WithMany(user => user.OrganisedEvents)
            .HasForeignKey(race => race.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(race => race.EventType)
            .WithMany(type => type.Events)
            .HasForeignKey(race => race.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(race => race.Location)
            .WithMany(location => location.Events)
            .HasForeignKey(race => race.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureCategories(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Category>();

        entity.ToTable("Categories", table =>
        {
            table.HasCheckConstraint("CK_Categories_Age", "[MinimumAge] IS NULL OR [MinimumAge] >= 0");
            table.HasCheckConstraint(
                "CK_Categories_MaxAge",
                "[MaximumAge] IS NULL OR [MaximumAge] >= [MinimumAge]");
            table.HasCheckConstraint(
                "CK_Categories_Participants",
                "[MaximumParticipants] IS NULL OR [MaximumParticipants] > 0");
        });

        entity.HasKey(category => category.CategoryId);
        entity.Property(category => category.CategoryName).HasMaxLength(100).IsRequired();
        entity.Property(category => category.CategoryDistanceKm).HasPrecision(6, 2);

        entity.HasOne(category => category.Event)
            .WithMany(race => race.Categories)
            .HasForeignKey(category => category.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureEnrolments(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Enrolment>();

        entity.ToTable("Enrolments", table =>
            table.HasCheckConstraint(
                "CK_Enrolments_Status",
                "[Status] IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')"));

        entity.HasKey(enrolment => enrolment.EnrolmentId);
        entity.Property(enrolment => enrolment.Status).HasMaxLength(20).IsRequired();
        ApplyTimestampDefault(entity.Property(enrolment => enrolment.EnrolmentDate));

        if (_sqlServer)
            entity.Property(enrolment => enrolment.Status).HasDefaultValue("Pending");

        entity.HasIndex(enrolment => new { enrolment.ParticipantId, enrolment.EventId })
            .IsUnique();

        entity.HasOne(enrolment => enrolment.Participant)
            .WithMany(user => user.Enrolments)
            .HasForeignKey(enrolment => enrolment.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(enrolment => enrolment.Event)
            .WithMany(race => race.Enrolments)
            .HasForeignKey(enrolment => enrolment.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(enrolment => enrolment.Category)
            .WithMany(category => category.Enrolments)
            .HasForeignKey(enrolment => enrolment.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureResults(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Result>();

        entity.ToTable("Results", table =>
            table.HasCheckConstraint("CK_Results_Position", "[FinishPosition] > 0"));

        entity.HasKey(result => result.ResultId);
        entity.Property(result => result.FinishTime).HasColumnType("time");
        ApplyTimestampDefault(entity.Property(result => result.RecordedAt));

        entity.HasIndex(result => result.EnrolmentId).IsUnique();

        entity.HasOne(result => result.Enrolment)
            .WithOne(enrolment => enrolment.Result)
            .HasForeignKey<Result>(result => result.EnrolmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureEventImages(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EventImage>();

        entity.ToTable("EventImages");
        entity.HasKey(image => image.EventImageId);
        entity.Property(image => image.ImageUrl).HasMaxLength(500).IsRequired();
        entity.Property(image => image.ImageType).HasMaxLength(50).IsRequired();
        ApplyTimestampDefault(entity.Property(image => image.UploadedAt));

        entity.HasOne(image => image.Event)
            .WithMany(race => race.Images)
            .HasForeignKey(image => image.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureWeather(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<WeatherSnapshot>();

        entity.ToTable("WeatherSnapshots", table =>
            table.HasCheckConstraint(
                "CK_Weather_Humidity",
                "[HumidityPercentage] BETWEEN 0 AND 100"));

        entity.HasKey(snapshot => snapshot.WeatherSnapshotId);
        entity.Property(snapshot => snapshot.TemperatureCelsius).HasPrecision(5, 2);
        entity.Property(snapshot => snapshot.FeelsLikeCelsius).HasPrecision(5, 2);
        entity.Property(snapshot => snapshot.WeatherCondition).HasMaxLength(100);
        entity.Property(snapshot => snapshot.WindSpeedKmh).HasPrecision(6, 2);
        ApplyTimestampDefault(entity.Property(snapshot => snapshot.RecordedAt));

        entity.HasOne(snapshot => snapshot.Event)
            .WithMany(race => race.WeatherSnapshots)
            .HasForeignKey(snapshot => snapshot.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ApplyTimestampDefault(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<DateTime> property)
    {
        property.HasColumnType("datetime");

        if (_sqlServer)
            property.HasDefaultValueSql("GETDATE()");
    }
}
