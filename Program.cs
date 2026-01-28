using McpVersionVer2.Models;
using McpVersionVer2.Services;
using McpVersionVer2.Services.Mappers;
using McpVersionVer2.Tools;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc; 
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// --- 1. LOGGING & CORE SERVICES ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddMemoryCache();

// --- 2. HTTP CLIENTS ---
builder.Services.AddHttpClient("AuthService", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "McpVersionVer2/1.0");
});
builder.Services.AddHttpClient(); 

// --- 3. CONFIG & DOMAIN SERVICES ---
builder.Services.Configure<ConversationConfig>(builder.Configuration.GetSection("ConversationContext"));

// Context & Session
builder.Services.AddSingleton<IConversationContextService, InMemoryConversationContextService>();
builder.Services.AddSingleton<ISessionStorageService, InMemorySessionStorageService>();
builder.Services.AddScoped<RequestContextService>();
builder.Services.AddSingleton<AuditLogService>();
builder.Services.AddSingleton<GitHubOpenAIService>();
builder.Services.AddSingleton<SecurityValidationService>();

// Vehicle & Mapping Services
builder.Services.AddTransient<VehicleMapperService>();
builder.Services.AddTransient<ContextTools>();

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
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthService"),
        sp.GetRequiredService<ILogger<AuthService>>(),
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ISessionStorageService>()
    ));

builder.Services.AddTransient<VehicleService>(sp =>
    new VehicleService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        sp.GetRequiredService<ILogger<VehicleService>>(),
        sp.GetRequiredService<IConfiguration>()
    ));

// --- 4. MCP SERVER SETUP ---
builder.Services.AddMcpServer()
    .WithPromptsFromAssembly()
    .WithHttpTransport() // Enables SSE logic
    .WithToolsFromAssembly();

// --- 5. CORS (Allowing Everything for Local Network Access) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Allows localhost, IPs, or ngrok
              .AllowAnyMethod()
              .AllowAnyHeader() // Critical for Mcp-Session-Id
              .AllowCredentials();
    });
});

var app = builder.Build();

// --- 6. MIDDLEWARE PIPELINE (Order Matters!) ---
app.UseCors("DefaultPolicy");

// Enable serving index.html automatically from wwwroot
app.UseDefaultFiles(); 
app.UseStaticFiles();

app.UseRouting();

// Start MCP Server (SSE Endpoint)
app.MapMcp();

// --- 7. CHAT PROXY ENDPOINT ---
app.MapPost("/api/chat", async (ChatRequest request, IHttpClientFactory clientFactory, IConfiguration config) => 
{
    Console.WriteLine("[Proxy] Received chat request...");

    try 
    {
        string secureToken = Environment.GetEnvironmentVariable("OPENAI_API_KEY"); 
        
        if (string.IsNullOrEmpty(secureToken) || secureToken == "YOUR_FALLBACK_TOKEN_IF_NEEDED")
        {
            Console.WriteLine("[Error] GitHub Token is missing in Configuration!");
            return Results.Problem("Configuration Error: GitHub Token is missing.");
        }

        // Prepare Client
        string azureUrl = "https://models.inference.ai.azure.com/chat/completions";
        var client = clientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secureToken}");

        Console.WriteLine("[Proxy] Forwarding to Azure...");
        
        // Forward the request to Azure
        var response = await client.PostAsJsonAsync(azureUrl, request);
        
        Console.WriteLine($"[Proxy] Azure Responded: {response.StatusCode}");
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Azure Error] {errorBody}");
            return Results.StatusCode((int)response.StatusCode);
        }

        var content = await response.Content.ReadAsStringAsync();
        return Results.Text(content, "application/json");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CRITICAL EXCEPTION] {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return Results.Problem(ex.Message);
    }
});

app.MapFallbackToFile("index.html");

app.Run("http://0.0.0.0:8080");
public record ChatRequest(object messages, object tools, string model);