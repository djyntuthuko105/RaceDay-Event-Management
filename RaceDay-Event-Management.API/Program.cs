using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Storage;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("RaceDayDb")
    ?? throw new InvalidOperationException(
        "The RaceDayDb connection string is missing. Add it under ConnectionStrings in appsettings.json.");

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // SameAsRequest keeps the cookie working on the http Swagger profile as well as https.
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddScoped<EventImageStorage>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "REST API for RaceDay. Log in with POST /api/auth/login first. " +
            "Swagger keeps the session cookie and sends it with the later requests."
    });

    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

await DatabaseStartup.ApplyAsync(app);

// Swagger stays on outside Development so the API can be tried from the browser during marking.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API");
    options.DocumentTitle = "RaceDay API";
});

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
