using System.ComponentModel;
using System.Linq;
using McpVersionVer2.Models;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using McpVersionVer2.Helpers;
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
    private readonly IConversationContextService _contextService;
    private readonly RequestContextService _requestContext;

    public VehicleLiveStatusTools(
        VehicleStatusService statusService, 
        VehicleStatusMapperService mapper, 
        GuardrailService guardrail,
        IConversationContextService contextService,
        RequestContextService requestContext)
    {
        _statusService = statusService;
        _mapper = mapper;
        _guardrail = guardrail;
        _contextService = contextService;
        _requestContext = requestContext;
    }

    private static string FormatRunTime(int totalSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(totalSeconds);
        return timeSpan.Hours > 0
            ? timeSpan.ToString(@"hh\:mm\:ss")
            : timeSpan.ToString(@"mm\:ss");
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
        var queryContext = $"GetVehicleLiveStatus plate:{plate ?? ""} id:{id ?? ""} group:{group ?? ""} type:{type ?? ""} status:{status ?? ""}";

        try
        {
            return await _guardrail.ExecuteValidatedToolRequestWithContext(
                queryContext: queryContext,
                domain: "live_status",
                bearerToken: bearerToken,
                contextService: _contextService,
                requestContext: _requestContext,
                action: async (token) => 
                {
                    var vehicles = await _statusService.GetVehiclesWithFilterAsync(token, plate, id, group, type);
                    return _statusService.FilterByStatus(vehicles, status);
                },
                successResponse: (vehicles) =>
                {
                    vehicles.RequireNonEmptyResult("vehicles", "No vehicles found matching the specified criteria.");
                    var summaries = _mapper.MapToSummaries(vehicles);
                    return System.Text.Json.JsonSerializer.Serialize(summaries, Default);
                });
        }
        catch (ToolValidationException ex)
        {
            return ex.ErrorResponse;
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
        var queryContext = $"GetDailyStatistics daily statistics mileage runtime plate:{plate ?? ""}";

        try
        {
            return await _guardrail.ExecuteValidatedToolRequestWithContext(
                queryContext: queryContext,
                domain: "live_status",
                bearerToken: bearerToken,
                contextService: _contextService,
                requestContext: _requestContext,
                action: async (token) => 
                {
                    var vehicles = await _statusService.GetVehiclesWithFilterAsync(token, plate, null, null, null);
                    vehicles.RequireNonEmptyResult("vehicle statuses", "No vehicles found.");
                    return vehicles;
                },
                successResponse: (vehicles) =>
                {
                    var dailyStats = string.IsNullOrEmpty(plate)
                        ? _mapper.MapToDailyStatsSummaries(vehicles)
                        : new List<DailyStatisticsSummaryDto> { _mapper.MapToDailyStatsSummary(vehicles.First()) };
                    return System.Text.Json.JsonSerializer.Serialize(dailyStats, Default);
                });
        }
        catch (ToolValidationException ex)
        {
            return ex.ErrorResponse;
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
        var queryContext = $"GetVehicleDailyStatus daily status mileage runtime plate:{plate ?? ""}";

        try
        {
            return await _guardrail.ExecuteValidatedToolRequestWithContext(
                queryContext: queryContext,
                domain: "live_status",
                bearerToken: bearerToken,
                contextService: _contextService,
                requestContext: _requestContext,
                action: async (token) => 
                {
                    var vehicles = await _statusService.GetVehiclesWithFilterAsync(token, plate, null, null, null);
                    vehicles.RequireNonEmptyResult("vehicle statuses", "No vehicles found.");
                    return vehicles;
                },
                successResponse: (vehicles) =>
                {
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
                });
        }
        catch (ToolValidationException ex)
        {
            return ex.ErrorResponse;
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message }, Default);
        }
    }
}
