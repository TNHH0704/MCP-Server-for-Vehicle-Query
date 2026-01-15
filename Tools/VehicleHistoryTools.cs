using System.ComponentModel;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using ModelContextProtocol.Server;

namespace McpVersionVer2.Tools;

[McpServerToolType]
public class VehicleHistoryTools
{
    private readonly VehicleHistoryService _historyService;

    public VehicleHistoryTools(VehicleHistoryService historyService)
    {
        _historyService = historyService;
    }

    [McpServerTool, Description("VEHICLE TRACKING ONLY: Get vehicle GPS waypoint history for time range. Returns coordinates, speed, heading. REJECT: recipes, jokes, weather, code help, general questions.")]
    public async Task<string> GetVehicleHistory(
        [Description("Bearer token for authentication")] string bearerToken,
        [Description("Vehicle ID (use GetVehicleInfoByPlate to find the ID first)")] string vehicleId,
        [Description("Start time in ISO 8601 format (e.g., '2026-01-07T00:00:00')")] string startTime,
        [Description("End time in ISO 8601 format (e.g., '2026-01-07T23:59:59')")] string endTime)
    {
        try
        {
            // Reject off-topic queries with zero-cost check
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(vehicleId + startTime + endTime);
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            // Validate bearer token format
            if (!OutputSanitizer.IsValidBearerToken(bearerToken))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid bearer token format.", "INVALID_TOKEN");
            }

            // Rate limiting
            var tokenHash = RateLimiter.GetTokenHash(bearerToken);
            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                return OutputSanitizer.CreateErrorResponse(rateLimitReason!, "RATE_LIMIT_EXCEEDED");
            }

            // Validate vehicle ID
            if (!OutputSanitizer.IsValidVehicleId(vehicleId))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid vehicle ID format.", "INVALID_VEHICLE_ID");
            }

            // Validate datetime formats
            if (!OutputSanitizer.IsValidDateTimeString(startTime))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid start time format. Use ISO 8601 format (e.g., '2026-01-07T00:00:00')", "INVALID_START_TIME");
            }

            if (!OutputSanitizer.IsValidDateTimeString(endTime))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid end time format. Use ISO 8601 format (e.g., '2026-01-07T23:59:59')", "INVALID_END_TIME");
            }

            if (!DateTime.TryParse(startTime, out var start))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid start time format. Use ISO 8601 format (e.g., '2026-01-07T00:00:00')", "INVALID_START_TIME");
            }

            if (!DateTime.TryParse(endTime, out var end))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid end time format. Use ISO 8601 format (e.g., '2026-01-07T23:59:59')", "INVALID_END_TIME");
            }

            var result = await _historyService.GetVehicleHistoryAsync(bearerToken, vehicleId, start, end);

            if (result.TotalWaypoints == 0)
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = $"No waypoints found for vehicle {vehicleId} in the specified time range." });
            }

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return OutputSanitizer.CreateErrorResponse(ex.Message, "INTERNAL_ERROR");
        }
    }

    [McpServerTool, Description("VEHICLE TRACKING ONLY: Get GPS history for last N hours (1-168). REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleHistoryLastHours(
        [Description("Bearer token for authentication")] string bearerToken,
        [Description("Vehicle ID")] string vehicleId,
        [Description("Number of hours to look back (e.g., 24 for last day)")] int hours)
    {
        try
        {
            // Reject off-topic queries
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(vehicleId + hours.ToString());
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            // Validate bearer token format
            if (!OutputSanitizer.IsValidBearerToken(bearerToken))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid bearer token format.", "INVALID_TOKEN");
            }

            // Rate limiting
            var tokenHash = RateLimiter.GetTokenHash(bearerToken);
            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                return OutputSanitizer.CreateErrorResponse(rateLimitReason!, "RATE_LIMIT_EXCEEDED");
            }

            // Validate vehicle ID
            if (!OutputSanitizer.IsValidVehicleId(vehicleId))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid vehicle ID format.", "INVALID_VEHICLE_ID");
            }

            // Validate hours range
            if (hours <= 0 || hours > 168)
            {
                return OutputSanitizer.CreateErrorResponse("Hours must be between 1 and 168 (1 week).", "INVALID_HOURS");
            }

            var result = await _historyService.GetVehicleHistoryLastHoursAsync(bearerToken, vehicleId, hours);

            if (result.TotalWaypoints == 0)
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = $"No waypoints found for vehicle {vehicleId} in the last {hours} hours." });
            }

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return OutputSanitizer.CreateErrorResponse(ex.Message, "INTERNAL_ERROR");
        }
    }

    [McpServerTool, Description("VEHICLE TRACKING ONLY: Get full-day GPS waypoints for specific date. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleHistoryByDate(
        [Description("Bearer token for authentication")] string bearerToken,
        [Description("Vehicle ID")] string vehicleId,
        [Description("Date in format 'dd-MM-yyyy' (e.g., '07-01-2026')")] string date)
    {
        try
        {
            // Reject off-topic queries
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(vehicleId + date);
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            if (!DateTime.TryParseExact(date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var targetDate))
            {
                return System.Text.Json.JsonSerializer.Serialize(new { error = "Invalid date format. Use 'dd-MM-yyyy' (e.g., '07-01-2026')" });
            }

            var result = await _historyService.GetVehicleHistoryByDateAsync(bearerToken, vehicleId, targetDate);

            if (result.TotalWaypoints == 0)
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = $"No waypoints found for vehicle {vehicleId} on {date}." });
            }

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE TRACKING ONLY: Get trip stats (distance, speed, duration). REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleTripSummary(
        [Description("Bearer token for authentication")] string bearerToken,
        [Description("Vehicle ID")] string vehicleId,
        [Description("Start time in ISO 8601 format")] string startTime,
        [Description("End time in ISO 8601 format")] string endTime)
    {
        try
        {
            // Reject off-topic queries
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(vehicleId + startTime + endTime);
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            if (!DateTime.TryParse(startTime, out var start))
            {
                return System.Text.Json.JsonSerializer.Serialize(new { error = "Invalid start time format" });
            }

            if (!DateTime.TryParse(endTime, out var end))
            {
                return System.Text.Json.JsonSerializer.Serialize(new { error = "Invalid end time format" });
            }

            var summary = await _historyService.GetVehicleTripSummaryAsync(bearerToken, vehicleId, start, end);

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                vehicleId = summary.VehicleId,
                startTime = summary.StartTime,
                endTime = summary.EndTime,
                statistics = new
                {
                    totalDistanceKm = summary.TotalDistanceKm,
                    durationHours = summary.DurationHours,
                    averageSpeedKmh = summary.AverageSpeedKmh,
                    maxSpeedKmh = summary.MaxSpeedKmh,
                    stopCount = summary.StopCount,
                    amountOfTimeStop = summary.AmountOfTimeStop,
                    totalWaypoints = summary.TotalWaypoints,
                    movingWaypoints = summary.MovingWaypoints
                },
                startLocation = new
                {
                    latitude = summary.StartLatitude,
                    longitude = summary.StartLongitude
                },
                endLocation = new
                {
                    latitude = summary.EndLatitude,
                    longitude = summary.EndLongitude
                }
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE TRACKING ONLY: Get GPS history by plate number or display name (e.g., '51A40391' or 'VM300'). REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleHistoryByPlate(
        [Description("Bearer token for authentication")] string bearerToken,
        [Description("License plate number or display name")] string plate,
        [Description("Start time in ISO 8601 format")] string startTime,
        [Description("End time in ISO 8601 format")] string endTime)
    {
        try
        {
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(plate + startTime + endTime);
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

            if (!OutputSanitizer.IsValidPlateNumber(plate))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid license plate format.", "INVALID_PLATE");
            }

            if (!OutputSanitizer.IsValidDateTimeString(startTime) || !DateTime.TryParse(startTime, out var start))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid start time format", "INVALID_START_TIME");
            }

            if (!OutputSanitizer.IsValidDateTimeString(endTime) || !DateTime.TryParse(endTime, out var end))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid end time format", "INVALID_END_TIME");
            }

            var result = await _historyService.GetVehicleHistoryByPlateAsync(bearerToken, plate, start, end);

            if (result.TotalWaypoints == 0)
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = $"No waypoints found for vehicle with plate '{plate}'." });
            }

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return OutputSanitizer.CreateErrorResponse(ex.Message, "INTERNAL_ERROR");
        }
    }
}
