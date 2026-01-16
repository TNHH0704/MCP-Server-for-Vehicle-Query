using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddHttpClient();
builder.Services.AddTransient<McpVersionVer2.Services.VehicleMapperService>();

builder.Services.AddTransient<McpVersionVer2.Services.VehicleStatusService>(sp => 
    new McpVersionVer2.Services.VehicleStatusService(sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(), sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()));

builder.Services.AddTransient<McpVersionVer2.Services.VehicleStatusMapperService>();

builder.Services.AddTransient<McpVersionVer2.Services.WaypointService>(sp => 
    new McpVersionVer2.Services.WaypointService(sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<McpVersionVer2.Services.WaypointService>>(), sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()));

builder.Services.AddTransient<McpVersionVer2.Services.VehicleHistoryService>();

builder.Services.AddTransient<McpVersionVer2.Services.AuthService>(sp => 
    new McpVersionVer2.Services.AuthService(sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<McpVersionVer2.Services.AuthService>>(), sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()));

builder.Services.AddTransient<McpVersionVer2.Services.VehicleService>(sp => 
    new McpVersionVer2.Services.VehicleService(sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<McpVersionVer2.Services.VehicleService>>(), sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()));

builder.Services.AddTransient<McpVersionVer2.Services.GisUtil>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
