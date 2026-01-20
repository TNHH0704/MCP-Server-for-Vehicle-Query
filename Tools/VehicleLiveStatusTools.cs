using System.ComponentModel;
using System.Linq;
using McpVersionVer2.Models;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using ModelContextProtocol.Server;
using static McpVersionVer2.Services.AppJsonSerializerOptions;

namespace McpVersionVer2.Tools;

[McpServerToolType]
public class VehicleLiveStatusTools
{
    private const double DISTANCE_DIVISOR = 1000.0;
    private const double SPEED_DIVISOR = 100.0;

    private readonly VehicleStatusService _statusService;
    private readonly VehicleStatusMapperService _mapper;
    private readonly GuardrailService _guardrail;

    public VehicleLiveStatusTools(VehicleStatusService statusService, VehicleStatusMapperService mapper, GuardrailService guardrail)
    {
        _statusService = statusService;
        _mapper = mapper;
        _guardrail = guardrail;
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS: Get real-time vehicle status. Supports: all vehicles, by plate, by ID, by group, by type, or filtered by status (all, moving, stopped, idle, overspeeding). Returns speed, location, heading, and status info. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleLiveStatus(
        [Description("Bearer token")] string bearerToken,
        [Description("Filter by plate number. Optional.")] string? plate = null,
        [Description("Filter by vehicle ID. Optional.")] string? id = null,
        [Description("Filter by group name. Optional.")] string? group = null,
        [Description("Filter by vehicle type (e.g., 'Xe máy'). Optional.")] string? type = null,
        [Description("Filter by status: 'all', 'moving', 'stopped', 'idle', 'overspeeding'. Default: 'all'.")] string? status = null)
    {
        var queryContext = $"plate:{plate ?? ""} id:{id ?? ""} group:{group ?? ""} type:{type ?? ""} status:{status ?? ""}";
        var userId = RateLimiter.GetTokenHash(bearerToken);
        
        var validation = _guardrail.ValidateQuery(queryContext, "live_status", userId);
        if (!validation.IsValid)
        {
            return validation.ToJsonResponse();
        }

        try
        {
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(queryContext);
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            if (!OutputSanitizer.IsValidBearerToken(bearerToken))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid bearer token format.", "INVALID_TOKEN");
            }

            var tokenHash = RateLimiter.GetTokenHash(bearerToken);
            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                return OutputSanitizer.CreateErrorResponse(rateLimitReason!, "RATE_LIMIT_EXCEEDED");
            }

            List<VehicleStatus> vehicles;

            if (!string.IsNullOrEmpty(plate))
            {
                var vehicle = await _statusService.GetVehicleStatusByPlateAsync(bearerToken, plate)
                    .SafeGetSingleAsync("vehicle", $"plate '{plate}'");
                vehicles = new List<VehicleStatus> { vehicle };
            }
            else if (!string.IsNullOrEmpty(id))
            {
                var vehicle = await _statusService.GetVehicleStatusByIdAsync(bearerToken, id)
                    .SafeGetSingleAsync("vehicle", $"ID '{id}'");
                vehicles = new List<VehicleStatus> { vehicle };
            }
            else if (!string.IsNullOrEmpty(group))
            {
                vehicles = await _statusService.GetVehiclesByGroupAsync(bearerToken, group);
            }
            else if (!string.IsNullOrEmpty(type))
            {
                vehicles = await _statusService.GetVehiclesByTypeAsync(bearerToken, type);
            }
            else
            {
                vehicles = await _statusService.GetVehicleStatusesAsync(bearerToken);
            }

            var statusFilter = status?.ToLowerInvariant();
            if (statusFilter == "moving")
            {
                vehicles = await _statusService.GetMovingVehiclesAsync(bearerToken);
            }
            else if (statusFilter == "stopped")
            {
                vehicles = await _statusService.GetStoppedVehiclesAsync(bearerToken);
            }
            else if (statusFilter == "idle")
            {
                vehicles = await _statusService.GetIdleVehiclesAsync(bearerToken);
            }
            else if (statusFilter == "overspeeding")
            {
                vehicles = await _statusService.GetOverSpeedingVehiclesAsync(bearerToken);
            }

            if (vehicles == null || !vehicles.Any())
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = "No vehicles found matching the specified criteria." }, Default);
            }

            var summaries = _mapper.MapToSummaries(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(summaries, Default);
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message }, Default);
        }
    }

    [McpServerTool, Description("VEHICLE DAILY STATS: Get daily statistics (mileage, runtime, max speed, overspeed count, engine off count). Returns GPS mileage, run time, max speed, over-speed events, and stop counts. Optional: filter by plate. REJECT: non-vehicle queries.")]
    public async Task<string> GetDailyStatistics(
        [Description("Bearer token")] string bearerToken,
        [Description("Filter by plate number. Optional.")] string? plate = null)
    {
        var queryContext = $"daily statistics mileage runtime plate:{plate ?? ""}";
        var userId = RateLimiter.GetTokenHash(bearerToken);
        
        var validation = _guardrail.ValidateQuery(queryContext, "live_status", userId);
        if (!validation.IsValid)
        {
            return validation.ToJsonResponse();
        }

        try
        {
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(plate ?? "");
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            if (!OutputSanitizer.IsValidBearerToken(bearerToken))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid bearer token format.", "INVALID_TOKEN");
            }

            var tokenHash = RateLimiter.GetTokenHash(bearerToken);
            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                return OutputSanitizer.CreateErrorResponse(rateLimitReason!, "RATE_LIMIT_EXCEEDED");
            }

            List<VehicleStatus> vehicles;

            if (!string.IsNullOrEmpty(plate))
            {
                var vehicle = await _statusService.GetVehicleStatusByPlateAsync(bearerToken, plate)
                    .SafeGetSingleAsync("vehicle", $"plate '{plate}'");
                vehicles = new List<VehicleStatus> { vehicle };
            }
            else
            {
                vehicles = await _statusService.GetVehicleStatusesAsync(bearerToken)
                    .SafeGetListAsync("vehicle statuses");
            }

            if (vehicles == null || !vehicles.Any())
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = "No vehicles found." }, Default);
            }

            var dailyStats = string.IsNullOrEmpty(plate)
                ? _mapper.MapToDailyStatsSummaries(vehicles)
                : new List<McpVersionVer2.Models.DailyStatisticsSummaryDto> { _mapper.MapToDailyStatsSummary(vehicles.First()) };

            return System.Text.Json.JsonSerializer.Serialize(dailyStats, Default);
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message }, Default);
        }
    }

    [McpServerTool, Description("VEHICLE DAILY STATUS: Get daily status summary (mileage, runtime, max speed, over-speed count, engine off count, vehicle stop count). Optional: filter by plate. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleDailyStatus(
        [Description("Bearer token")] string bearerToken,
        [Description("Filter by plate number. Optional - returns all if not specified.")] string? plate = null)
    {
        var queryContext = $"daily status mileage runtime plate:{plate ?? ""}";
        var userId = RateLimiter.GetTokenHash(bearerToken);
        
        var validation = _guardrail.ValidateQuery(queryContext, "live_status", userId);
        if (!validation.IsValid)
        {
            return validation.ToJsonResponse();
        }

        try
        {
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(plate ?? "");
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            if (!OutputSanitizer.IsValidBearerToken(bearerToken))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid bearer token format.", "INVALID_TOKEN");
            }

            var tokenHash = RateLimiter.GetTokenHash(bearerToken);
            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                return OutputSanitizer.CreateErrorResponse(rateLimitReason!, "RATE_LIMIT_EXCEEDED");
            }

            List<VehicleStatus> vehicles;

            if (!string.IsNullOrEmpty(plate))
            {
                var vehicle = await _statusService.GetVehicleStatusByPlateAsync(bearerToken, plate)
                    .SafeGetSingleAsync("vehicle", $"plate '{plate}'");
                vehicles = new List<VehicleStatus> { vehicle };
            }
            else
            {
                vehicles = await _statusService.GetVehicleStatusesAsync(bearerToken)
                    .SafeGetListAsync("vehicle statuses");
            }

            if (vehicles == null || !vehicles.Any())
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = "No vehicles found." }, Default);
            }

            var dailyStatus = vehicles.Select(v => new
            {
                plate = v.Plate,
                displayName = v.CustomPlateNumber,
                gpsMileage = $"{(v.Daily?.GpsMileage ?? 0) / DISTANCE_DIVISOR:F2} km",
                runTime = FormatRunTime(v.Daily?.RunTime ?? 0),
                maxSpeed = $"{(v.Daily?.MaxSpeed ?? 0) / SPEED_DIVISOR:F1} km/h",
                overSpeedCount = v.Daily?.OverSpeed ?? 0,
                engineOffCount = v.Daily?.StopCount ?? 0,
                vehicleStopCount = v.Daily?.IdleCount ?? 0
            }).ToList();

            return System.Text.Json.JsonSerializer.Serialize(dailyStatus, Default);
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message }, Default);
        }
    }

    private static string FormatRunTime(int totalSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(totalSeconds);
        return timeSpan.Hours > 0
            ? timeSpan.ToString(@"hh\:mm\:ss")
            : timeSpan.ToString(@"mm\:ss");
    }
}
