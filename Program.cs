using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using McpVersionVer2.Models;
using McpVersionVer2.Services;
using McpVersionVer2.Tools;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.Configure<ConversationConfig>(builder.Configuration.GetSection("ConversationContext"));
builder.Services.AddSingleton<IConversationContextService, InMemoryConversationContextService>();
builder.Services.AddSingleton<ISessionStorageService, InMemorySessionStorageService>();
builder.Services.AddScoped<RequestContextService>();
builder.Services.AddTransient<ContextTools>();
builder.Services.AddSingleton<AuditLogService>();
builder.Services.AddSingleton<GitHubOpenAIService>();
builder.Services.AddSingleton<SecurityValidationService>();
builder.Services.AddTransient<VehicleMapperService>();

builder.Services.AddTransient<VehicleStatusService>(sp =>
    new VehicleStatusService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        sp.GetRequiredService<IConfiguration>()
    ));

    builder.Services.AddTransient<VehicleStatusMapperService>(sp =>
        new VehicleStatusMapperService(sp.GetRequiredService<SecurityValidationService>())
    );

builder.Services.AddTransient<WaypointService>(sp =>
    new WaypointService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        sp.GetRequiredService<ILogger<WaypointService>>(),
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IMemoryCache>()
    ));

builder.Services.AddTransient<VehicleHistoryService>();

builder.Services.AddTransient<AuthService>(sp =>
    new AuthService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        sp.GetRequiredService<ILogger<AuthService>>(),
        sp.GetRequiredService<IConfiguration>()
    ));

builder.Services.AddTransient<VehicleService>(sp =>
    new VehicleService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        sp.GetRequiredService<ILogger<VehicleService>>(),
        sp.GetRequiredService<IConfiguration>()
    ));

builder.Services.AddTransient<GisUtil>();

builder.Services.AddMcpServer()
    .WithPromptsFromAssembly()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            // REPLACE with your actual Frontend URL
            policy.WithOrigins("http://0.0.0.0:8080")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Important if using Auth Cookies
        });
});

var app = builder.Build();

app.UseCors(myAllowSpecificOrigins);

app.MapMcp();

app.Run("http://0.0.0.0:8080");