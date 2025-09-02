using Patlite.Service;

using Serilog;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

// Service
builder.Services
    .AddWindowsService()
    .AddSystemd();

// Logging
builder.Logging.ClearProviders();
builder.Services.AddSerilog(options =>
{
    options.ReadFrom.Configuration(builder.Configuration);
});

// Setting
builder.Services.Configure<PatliteSetting>(builder.Configuration.GetSection("Patlite"));

// Service
builder.Services.AddSingleton<PatliteService>();

// Build
var app = builder.Build();

// End points
app.MapGet("/", () => "Patlite service.");
app.MapPost("/write", (LightRequest request, PatliteService service) =>
{
    service.Write(request.Color, request.Blink, request.Wait);
    return Results.Ok();
});
app.MapPost("/cancel", (PatliteService service) =>
{
    service.Cancel();
    return Results.Ok();
});

var log = app.Services.GetRequiredService<ILogger<Program>>();

// Startup information
ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
log.InfoServiceStart();
log.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.Version, Environment.CurrentDirectory);
log.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
log.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);

// Run
await app.RunAsync();

internal sealed record LightRequest(string Color, bool Blink, int Wait);
