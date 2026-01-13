using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
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

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly)
    .WithPromptsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// Enable CORS before other middleware
app.UseCors();

// Start background task to clean up rate limiter
var cleanupTask = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromHours(1));
        McpVersionVer2.Security.RateLimiter.Cleanup();
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Rate limiter cleanup completed");
    }
});

// Map MCP endpoints with optional path prefix
app.MapMcp("/sse");

await app.RunAsync();