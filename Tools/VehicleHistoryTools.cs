using System.ComponentModel;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using ModelContextProtocol.Server;

namespace McpVersionVer2.Tools;

[McpServerToolType]
public class VehicleHistoryTools
{
    private readonly VehicleHistoryService _historyService;
    private readonly VehicleService _vehicleService;

    public VehicleHistoryTools(VehicleHistoryService historyService, VehicleService vehicleService)
    {
        _historyService = historyService;
        _vehicleService = vehicleService;
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
        try
        {
            var validationInput = $"{vehicleId ?? plate}{startTime ?? ""}{endTime ?? ""}{hours?.ToString() ?? ""}{date ?? ""}";
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(validationInput);
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

            DateTime start;
            DateTime end;
            int hoursBack = 0;
            string? dateStr = null;

            if (!string.IsNullOrEmpty(plate))
            {
                if (!OutputSanitizer.IsValidPlateNumber(plate))
                {
                    return OutputSanitizer.CreateErrorResponse("Invalid license plate format.", "INVALID_PLATE");
                }

                var vehicle = await _vehicleService.GetVehicleByPlateAsync(bearerToken, plate)
                    .SafeGetSingleAsync("vehicle", $"plate '{plate}'");
                vehicleId = vehicle.Id;
            }

            if (!string.IsNullOrEmpty(date))
            {
                if (!DateTime.TryParseExact(date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var targetDate))
                {
                    return System.Text.Json.JsonSerializer.Serialize(new { error = "Invalid date format. Use 'dd-MM-yyyy' (e.g., '07-01-2026')" });
                }
                start = targetDate.Date;
                end = start.AddDays(1).AddSeconds(-1);
                dateStr = date;
            }
            else if (hours.HasValue)
            {
                if (hours.Value <= 0 || hours.Value > 168)
                {
                    return OutputSanitizer.CreateErrorResponse("Hours must be between 1 and 168 (1 week).", "INVALID_HOURS");
                }
                end = DateTime.UtcNow;
                start = end.AddHours(-hours.Value);
                hoursBack = hours.Value;
            }
            else
            {
                if (string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime))
                {
                    return OutputSanitizer.CreateErrorResponse("Either startTime+endTime, hours, or date must be provided.", "INVALID_TIME_RANGE");
                }

                if (!OutputSanitizer.IsValidDateTimeString(startTime) || !DateTime.TryParse(startTime, out start))
                {
                    return OutputSanitizer.CreateErrorResponse("Invalid start time format. Use ISO 8601 format (e.g., '2026-01-07T00:00:00')", "INVALID_START_TIME");
                }

                if (!OutputSanitizer.IsValidDateTimeString(endTime) || !DateTime.TryParse(endTime, out end))
                {
                    return OutputSanitizer.CreateErrorResponse("Invalid end time format. Use ISO 8601 format (e.g., '2026-01-07T23:59:59')", "INVALID_END_TIME");
                }
            }

            if (!string.IsNullOrEmpty(vehicleId) && !OutputSanitizer.IsValidVehicleId(vehicleId))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid vehicle ID format.", "INVALID_VEHICLE_ID");
            }

            var result = await _historyService.GetVehicleHistoryAsync(bearerToken, vehicleId!, start, end);
            result.HoursBack = hoursBack;
            result.Date = dateStr;

            if (result.TotalWaypoints == 0)
            {
                var queryDesc = !string.IsNullOrEmpty(date) ? $"on {date}" :
                               hours.HasValue ? $"in the last {hours} hours" :
                               $"between {start:yyyy-MM-dd HH:mm} and {end:yyyy-MM-dd HH:mm}";
                return System.Text.Json.JsonSerializer.Serialize(new { message = $"No waypoints found for vehicle {vehicleId} {queryDesc}." });
            }

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                vehicleId = result.VehicleId,
                startTime = result.StartTime,
                endTime = result.EndTime,
                hoursBack = result.HoursBack,
                date = result.Date,
                summary = new
                {
                    totalWaypoints = result.TotalWaypoints,
                    movingWaypoints = result.MovingWaypoints,
                    totalDistanceKm = result.TotalDistanceKm,
                    totalRunningTimeHours = result.TotalRunningTimeHours,
                    totalStopTimeHours = result.TotalStopTimeHours,
                    averageSpeedKmh = result.AverageSpeedKmh,
                    highestSpeedKmh = result.HighestSpeedKmh,
                    stopCount = result.AmountOfTimeStop
                },
                waypoints = result.Waypoints
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return OutputSanitizer.CreateErrorResponse(ex.Message, "INTERNAL_ERROR");
        }
    }

    [McpServerTool, Description("VEHICLE TRACKING ONLY: Get trip summary statistics (distance, speed, duration, start/end locations) for a vehicle over a time range. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleTripSummary(
        [Description("Bearer token for authentication")] string bearerToken,
        [Description("Vehicle ID (use GetVehicleInfo to find ID first)")] string vehicleId,
        [Description("Start time in ISO 8601 format (e.g., '2026-01-07T00:00:00')")] string startTime,
        [Description("End time in ISO 8601 format (e.g., '2026-01-07T23:59:59')")] string endTime)
    {
        try
        {
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(vehicleId + startTime + endTime);
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

            if (!OutputSanitizer.IsValidVehicleId(vehicleId))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid vehicle ID format.", "INVALID_VEHICLE_ID");
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
                    duration = summary.DurationHours,
                    averageSpeedKmh = summary.AverageSpeedKmh,
                    maxSpeedKmh = summary.MaxSpeedKmh,
                    stopCount = summary.StopCount,
                    runningTime = TimeSpan.FromHours(summary.AmountOfTimeRunning).ToString(@"hh\:mm\:ss"),
                    stopTime = TimeSpan.FromHours(summary.AmountOfTimeStop).ToString(@"hh\:mm\:ss"),
                    totalWaypoints = summary.TotalWaypoints,
                    movingWaypoints = summary.MovingWaypoints
                },
                startLocation = new
                {
                    latitude = summary.StartLatitude,
                    longitude = summary.StartLongitude,
                    info = summary.StartInfo
                },
                endLocation = new
                {
                    latitude = summary.EndLatitude,
                    longitude = summary.EndLongitude,
                    info = summary.EndInfo
                }
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
