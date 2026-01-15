using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    // CRITICAL: Force all logs to stderr so they don't interfere with StdIO MCP transport on stdout
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Configure to listen on all interfaces
builder.WebHost.UseUrls("http://0.0.0.0:3001");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("*");
    });
});

// Register services
builder.Services.AddHttpClient<McpVersionVer2.Services.VehicleService>();
builder.Services.AddScoped<McpVersionVer2.Services.VehicleMapperService>();

// Register vehicle status services
builder.Services.AddHttpClient<McpVersionVer2.Services.VehicleStatusService>();
builder.Services.AddScoped<McpVersionVer2.Services.VehicleStatusMapperService>();

// Register waypoint/history services
builder.Services.AddHttpClient<McpVersionVer2.Services.WaypointService>();
builder.Services.AddScoped<McpVersionVer2.Services.VehicleHistoryService>();

// Register authentication services
builder.Services.AddHttpClient<McpVersionVer2.Services.AuthService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly)
    .WithPromptsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// Enable CORS before other middleware
app.UseCors();

// Start background task to clean up rate limiter with proper error handling
var cleanupCancellation = new CancellationTokenSource();
_ = Task.Run(async () =>
{
    while (!cleanupCancellation.Token.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(TimeSpan.FromHours(1), cleanupCancellation.Token);
            McpVersionVer2.Security.RateLimiter.Cleanup();
            Console.Error.WriteLine($"[{DateTime.UtcNow:dd-MM-yyyy HH:mm:ss}] Rate limiter cleanup completed");
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
            break;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[{DateTime.UtcNow:dd-MM-yyyy HH:mm:ss}] Rate limiter cleanup failed: {ex.Message}");
        }
    }
}, cleanupCancellation.Token);

// Register cancellation on shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() => cleanupCancellation.Cancel());


// Map MCP endpoints with optional path prefix
app.MapMcp("/mcp");

await app.RunAsync();