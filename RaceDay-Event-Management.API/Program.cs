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
{
    if (builder.Configuration["Database:Provider"] == "SQLite") options.UseSqlite(connectionString);
    else options.UseSqlServer(connectionString);
});

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
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<RaceDay_Event_Management.API.Services.CurrentConditionsService>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(4);
});
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
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new RaceDay_Event_Management.API.Contracts.ApiMessage("The service could not complete this request. Please try again shortly."));
}));
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next();
});

// Swagger stays on outside Development so the API can be tried from the browser during marking.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API");
    options.DocumentTitle = "RaceDay API";
});

if (!app.Environment.IsEnvironment("Testing") && app.Configuration.GetValue<bool>("Https:Enabled"))
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ready" }));

app.Run();

public partial class Program
{
}
