using System.ComponentModel;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using McpVersionVer2.Helpers;
using ModelContextProtocol.Server;
using static McpVersionVer2.Services.AppJsonSerializerOptions;

namespace McpVersionVer2.Tools;

[McpServerToolType]
public class VehicleHistoryTools
{
    private readonly VehicleHistoryService _historyService;
    private readonly VehicleService _vehicleService;
    private readonly SecurityValidationService _securityService;
    private readonly IConversationContextService _contextService;
    private readonly RequestContextService _requestContext;

    public VehicleHistoryTools(
        VehicleHistoryService historyService, 
        VehicleService vehicleService, 
        SecurityValidationService securityService,
        IConversationContextService contextService,
        RequestContextService requestContext)
    {
        _historyService = historyService;
        _vehicleService = vehicleService;
        _securityService = securityService;
        _contextService = contextService;
        _requestContext = requestContext;
        _requestContext = requestContext;
    }

    [McpServerTool, Description("VEHICLE TRACKING: Get GPS waypoint history for a vehicle. Supports multiple query modes: Time range (startTime+endTime), Last N hours (hours), By date (date), By plate. Returns coordinates, speed, heading, cumulative distance, and trip statistics. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleHistory(
        [Description("Bearer token for authentication")] string bearerToken,
        [Description("Vehicle ID (use GetVehicleInfo to find ID first). Optional if plate is provided.")] string? vehicleId = null,
        [Description("License plate number or display name (e.g., '51A40391'). Optional if vehicleId is provided.")] string? plate = null,
        [Description("Start time in ISO 8601 format (e.g., '2026-01-07T00:00:00'). Required unless using hours or date.")] string? startTime = null,
        [Description("End time in ISO 8601 format (e.g., '2026-01-07T23:59:59'). Required unless using hours or date.")] string? endTime = null,
        [Description("Number of hours to look back (1-168). Alternative to startTime/endTime.")] int? hours = null,
        [Description("Date in 'dd-MM-yyyy' format (e.g., '07-01-2026'). Alternative to time range.")] string? date = null)
    {
        string timeRange;
        if (!string.IsNullOrEmpty(date))
        {
            timeRange = $"date:{date}";
        }
        else if (hours.HasValue)
        {
            timeRange = $"hours:{hours}";
        }
        else
        {
            timeRange = $"time:{startTime ?? ""}-{endTime ?? ""}";
        }
        var queryContext = $"GetVehicleHistory vehicle:{vehicleId ?? plate} {timeRange}";

        try
        {
            return await _securityService.ExecuteValidatedToolRequestWithContext(
                queryContext: queryContext,
                domain: "history",
                bearerToken: bearerToken,
                contextService: _contextService,
                requestContext: _requestContext,
                action: async (token) => 
                {
                    DateTime start = DateTime.UtcNow;
                    DateTime end = DateTime.UtcNow;
                    
                    if (!string.IsNullOrEmpty(plate))
                    {
                        if (!_securityService.IsValidPlateNumber(plate))
                        {
                            throw new ArgumentException("Invalid license plate format.", nameof(plate));
                        }

                        var vehicle = await _vehicleService.GetVehicleByPlateAsync(token, plate)
                            .SafeGetSingleAsync("vehicle", $"plate '{plate}'");
                        vehicleId = vehicle!.Id;
                    }

                    if (!string.IsNullOrEmpty(date))
                    {
                        if (!DateTime.TryParseExact(date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var dateValue))
                        {
                            throw new ArgumentException("Date must be in dd-MM-yyyy format (e.g., '20-01-2026').", nameof(date));
                        }
                        start = dateValue.Date; 
                        end = dateValue.Date.AddDays(1).AddSeconds(-1); 
                    }
                    else if (hours.HasValue)
                    {
                        end = DateTime.UtcNow;
                        start = end.AddHours(-hours.Value);
                        if (hours.Value < 1 || hours.Value > 168)
                        {
                            throw new ArgumentException("Hours parameter must be between 1 and 168.", nameof(hours));
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(startTime) && !DateTime.TryParse(startTime, out start))
                        {
                            throw new ArgumentException("Invalid start time format.", nameof(startTime));
                        }

                        if (!string.IsNullOrEmpty(endTime) && !DateTime.TryParse(endTime, out end))
                        {
                            throw new ArgumentException("Invalid end time format.", nameof(endTime));
                        }
                    }

                    if (!string.IsNullOrEmpty(vehicleId) && !_securityService.IsValidVehicleId(vehicleId))
                    {
                        throw new ArgumentException("Invalid vehicle ID format.", nameof(vehicleId));
                    }

                    return await _historyService.GetVehicleHistoryAsync(token, vehicleId!, start, end);
                },
                successResponse: (result) => System.Text.Json.JsonSerializer.Serialize(result, Default));
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

    [McpServerTool, Description("VEHICLE TRACKING ONLY: Get trip summary statistics (distance, speed, duration, start/end locations) for a vehicle over a time range. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleTripSummary(
        [Description("Bearer token for authentication")] string bearerToken,
        [Description("Vehicle ID (use GetVehicleInfo to find ID first)")] string vehicleId,
        [Description("Start time in ISO 8601 format (e.g., '2026-01-07T00:00:00')")] string startTime,
        [Description("End time in ISO 8601 format (e.g., '2026-01-07T23:59:59')")] string endTime)
    {
        var queryContext = $"GetVehicleTripSummary vehicle:{vehicleId} from:{startTime} to:{endTime}";

        try
        {
            return await _securityService.ExecuteValidatedToolRequestWithContext(
                queryContext: queryContext,
                domain: "history",
                bearerToken: bearerToken,
                contextService: _contextService,
                requestContext: _requestContext,
                action: async (token) => 
                {
                    if (!_securityService.IsValidVehicleId(vehicleId))
                    {
                        throw new ArgumentException("Invalid vehicle ID format.", nameof(vehicleId));
                    }

                    if (!DateTime.TryParse(startTime, out var start))
                    {
                        throw new ArgumentException("Invalid start time format", nameof(startTime));
                    }

                    if (!DateTime.TryParse(endTime, out var end))
                    {
                        throw new ArgumentException("Invalid end time format", nameof(endTime));
                    }

                    return await _historyService.GetVehicleTripSummaryAsync(token, vehicleId, start, end);
                },
                successResponse: (result) => System.Text.Json.JsonSerializer.Serialize(result, Default));
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
