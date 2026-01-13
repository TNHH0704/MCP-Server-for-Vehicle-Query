using Microsoft.Extensions.Logging;

namespace McpVersionVer2.Services;

public class VehicleHistoryService
{
    private readonly WaypointService _waypointService;
    private readonly VehicleService _vehicleService;
    private readonly ILogger<VehicleHistoryService> _logger;

    // GPS tracking systems typically use 2010-01-01 as epoch instead of Unix epoch (1970-01-01)
    // Using Local time to match the system's timezone
    private static readonly DateTime GpsEpoch = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Local);

    public VehicleHistoryService(
        WaypointService waypointService,
        VehicleService vehicleService,
        ILogger<VehicleHistoryService> logger)
    {
        _waypointService = waypointService;
        _vehicleService = vehicleService;
        _logger = logger;
    }

    /// <summary>
    /// Convert GPS time (seconds since 2010-01-01) to DateTime
    /// </summary>
    public static DateTime ConvertGpsTimeToDateTime(int gpsTime)
    {
        return GpsEpoch.AddSeconds(gpsTime);
    }

    /// <summary>
    /// Format seconds to HH:MM:SS
    /// </summary>
    private static string FormatTimeHHMMSS(int totalSeconds)
    {
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    /// <summary>
    /// Calculate distance between two GPS coordinates using Haversine formula
    /// </summary>
    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in kilometers
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// Truncate a number to 3 decimal places (ignore digits after 3rd decimal)
    /// </summary>
    private static double TruncateTo3Decimals(double value)
    {
        return Math.Truncate(value * 1000) / 1000;
    }

    /// <summary>
    /// Truncate a number to 6 decimal places (ignore digits after 6th decimal)
    /// </summary>
    private static double TruncateTo6Decimals(double value)
    {
        return Math.Truncate(value * 1000000) / 1000000;
    }

    #region Helper Methods

    /// <summary>
    /// Validate inputs and adjust time range if needed
    /// </summary>
    private static void ValidateInputs(string vehicleId, DateTime startTime, ref DateTime endTime)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
        {
            throw new ArgumentException("Vehicle ID cannot be empty or whitespace.", nameof(vehicleId));
        }

        // If start and end times are the same, expand to a 1-minute window
        if (startTime == endTime)
        {
            endTime = endTime.AddMinutes(1);
        }
        else if (startTime > endTime)
        {
            throw new ArgumentException("Start time must be before end time. Please provide a valid time range.");
        }
    }

    /// <summary>
    /// Create an empty history result when no waypoints are found
    /// </summary>
    private static VehicleHistoryResult CreateEmptyHistoryResult(string vehicleId, DateTime startTime, DateTime endTime)
    {
        return new VehicleHistoryResult
        {
            VehicleId = vehicleId,
            StartTime = startTime,
            EndTime = endTime,
            TotalWaypoints = 0,
            MovingWaypoints = 0,
            Waypoints = new List<WaypointSummary>(),
            TotalDistanceKm = 0,
            TotalRunningTimeHours = 0,
            TotalStopTimeHours = 0,
            AmountOfTimeStop = 0,
            AverageSpeedKmh = 0,
            HighestSpeedKmh = 0
        };
    }

    /// <summary>
    /// Convert waypoints to summary format with proper unit conversions
    /// Includes cumulative distance calculation
    /// </summary>
    private List<WaypointSummary> ConvertToWaypointSummaries(List<Waypoint> waypoints)
    {
        var summaries = new List<WaypointSummary>();
        double cumulativeDistance = 0.0;

        for (int i = 0; i < waypoints.Count; i++)
        {
            var w = waypoints[i];
            
            // Calculate distance from previous waypoint
            if (i > 0)
            {
                var prev = waypoints[i - 1];
                var lat1 = prev.Y / 1000000.0;
                var lon1 = prev.X / 1000000.0;
                var lat2 = w.Y / 1000000.0;
                var lon2 = w.X / 1000000.0;

                // Calculate distance between all waypoints without filtering
                var distance = CalculateDistanceKm(lat1, lon1, lat2, lon2);
                
                // Truncate each segment distance before accumulating
                var truncatedDistance = TruncateTo3Decimals(distance);
                cumulativeDistance += truncatedDistance;
                // Truncate cumulative value to avoid floating-point precision issues
                cumulativeDistance = TruncateTo3Decimals(cumulativeDistance);
            }

            summaries.Add(new WaypointSummary
            {
                Timestamp = ConvertGpsTimeToDateTime(w.GpsTime).ToString("yyyy-MM-dd HH:mm:ss"),
                RawGpsTime = w.GpsTime,
                Latitude = TruncateTo6Decimals(w.Y / 1000000.0),
                Longitude = TruncateTo6Decimals(w.X / 1000000.0),
                Altitude = w.Z,
                Speed = w.Speed / 100.0, // Speed stored as speed * 100 (e.g., 500 = 5 km/h)
                Heading = w.Heading,
                Satellites = w.Satellite,
                Mileage = w.Mile / 1000.0,
                GpsMileage = w.GpsMile / 1000.0,
                CumulativeDistanceKm = cumulativeDistance,
                EventId = w.EventId,
                Status = w.Status,
                Voltage = w.Voltage / 1000.0,
                Battery = w.Battery,
                DriverId = w.DriverId,
                DriverCode = w.DriverCode,
                Info = w.Info
            });
        }

        return summaries;
    }

    /// <summary>
    /// Calculate all trip statistics from waypoints
    /// </summary>
    private (int MovingCount, double TotalRunningTimeHours, double TotalStopTimeHours, 
             string TotalRunningTimeFormatted, string TotalStopTimeFormatted, int StopCount, 
             double AverageSpeedKmh, double HighestSpeedKmh) CalculateTripStatistics(List<Waypoint> sortedWaypoints, double totalDistanceKm)
    {
        var movingCount = sortedWaypoints.Count(w => w.Speed > 0);
        var (runningTimeSeconds, stopTimeSeconds, stopCount) = CalculateRunningAndStopTime(sortedWaypoints);
        
        // Speed in waypoint is stored as speed * 100 (e.g., 500 = 5 km/h)
        var totalSpeed = sortedWaypoints.Sum(w => w.Speed / 100.0);
        var avgSpeed = sortedWaypoints.Count > 0 ? totalSpeed / sortedWaypoints.Count : 0;
        var maxSpeed = sortedWaypoints.Max(w => w.Speed / 100.0);

        return (
            MovingCount: movingCount,
            TotalRunningTimeHours: Math.Round(runningTimeSeconds / 3600.0, 2),
            TotalStopTimeHours: Math.Round(stopTimeSeconds / 3600.0, 2),
            TotalRunningTimeFormatted: FormatTimeHHMMSS(runningTimeSeconds),
            TotalStopTimeFormatted: FormatTimeHHMMSS(stopTimeSeconds),
            StopCount: stopCount,
            AverageSpeedKmh: Math.Round(avgSpeed, 2),
            HighestSpeedKmh: Math.Round(maxSpeed, 2)
        );
    }

    /// <summary>
    /// Check if GPS coordinates are valid (not 0,0)
    /// </summary>
    private static bool IsValidCoordinate(double lat, double lon)
    {
        return lat != 0 && lon != 0;
    }

    #endregion

    /// <summary>
    /// Get vehicle history for a specific time range
    /// </summary>
    public async Task<VehicleHistoryResult> GetVehicleHistoryAsync(
        string bearerToken,
        string vehicleId,
        DateTime startTime,
        DateTime endTime)
    {
        ValidateInputs(vehicleId, startTime, ref endTime);

        var waypoints = await _waypointService.GetVehicleWaypointsAsync(bearerToken, vehicleId, startTime, endTime);

        if (waypoints == null || !waypoints.Any())
        {
            return CreateEmptyHistoryResult(vehicleId, startTime, endTime);
        }

        var sortedWaypoints = waypoints.OrderBy(w => w.GpsTime).ToList();
        var waypointSummaries = ConvertToWaypointSummaries(sortedWaypoints);
        
        // Get total distance from the last waypoint's cumulative distance (already truncated)
        var totalDistanceKm = waypointSummaries.Any() ? waypointSummaries.Last().CumulativeDistanceKm : 0.0;
        
        var statistics = CalculateTripStatistics(sortedWaypoints, totalDistanceKm);

        return new VehicleHistoryResult
        {
            VehicleId = vehicleId,
            StartTime = startTime,
            EndTime = endTime,
            TotalWaypoints = waypoints.Count,
            MovingWaypoints = statistics.MovingCount,
            Waypoints = waypointSummaries,
            TotalDistanceKm = totalDistanceKm,
            TotalRunningTimeHours = statistics.TotalRunningTimeHours,
            TotalStopTimeHours = statistics.TotalStopTimeHours,
            TotalRunningTimeFormatted = statistics.TotalRunningTimeFormatted,
            TotalStopTimeFormatted = statistics.TotalStopTimeFormatted,
            AmountOfTimeStop = statistics.StopCount,
            AverageSpeedKmh = statistics.AverageSpeedKmh,
            HighestSpeedKmh = statistics.HighestSpeedKmh
        };
    }

    /// <summary>
    /// Get vehicle history for the last N hours
    /// </summary>
    public async Task<VehicleHistoryResult> GetVehicleHistoryLastHoursAsync(
        string bearerToken,
        string vehicleId,
        int hours)
    {
        if (hours <= 0 || hours > 168) // Max 1 week
        {
            throw new ArgumentException("Hours must be between 1 and 168 (1 week)");
        }

        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-hours);

        var result = await GetVehicleHistoryAsync(bearerToken, vehicleId, startTime, endTime);
        result.HoursBack = hours;
        return result;
    }

    /// <summary>
    /// Get vehicle history for a specific date (full day)
    /// </summary>
    public async Task<VehicleHistoryResult> GetVehicleHistoryByDateAsync(
        string bearerToken,
        string vehicleId,
        DateTime date)
    {
        var startTime = date.Date;
        var endTime = startTime.AddDays(1).AddSeconds(-1);

        var result = await GetVehicleHistoryAsync(bearerToken, vehicleId, startTime, endTime);
        result.Date = date.ToString("yyyy-MM-dd");
        return result;
    }

    /// <summary>
    /// Get vehicle trip summary statistics for a time range
    /// </summary>
    public async Task<VehicleTripSummary> GetVehicleTripSummaryAsync(
        string bearerToken,
        string vehicleId,
        DateTime startTime,
        DateTime endTime)
    {
        var waypoints = await _waypointService.GetVehicleWaypointsAsync(bearerToken, vehicleId, startTime, endTime);

        if (waypoints == null || !waypoints.Any())
        {
            throw new InvalidOperationException($"No waypoints found for vehicle {vehicleId}.");
        }

        var movingWaypoints = waypoints.Where(w => w.Speed > 0).ToList();

        if (!movingWaypoints.Any())
        {
            throw new InvalidOperationException($"No moving waypoints found for vehicle {vehicleId} (all waypoints have zero speed).");
        }

        var orderedWaypoints = movingWaypoints.OrderBy(w => w.GpsTime).ToList();
        var firstWaypoint = orderedWaypoints.First();
        var lastWaypoint = orderedWaypoints.Last();

        var durationSeconds = lastWaypoint.GpsTime - firstWaypoint.GpsTime;
        var speeds = movingWaypoints.Select(w => w.Speed / 100.0).ToList();

        return new VehicleTripSummary
        {
            VehicleId = vehicleId,
            StartTime = ConvertGpsTimeToDateTime(firstWaypoint.GpsTime).ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = ConvertGpsTimeToDateTime(lastWaypoint.GpsTime).ToString("yyyy-MM-dd HH:mm:ss"),
            TotalDistanceKm = Math.Round((lastWaypoint.Mile - firstWaypoint.Mile) / 1000.0, 3),
            TotalGpsDistanceKm = Math.Round((lastWaypoint.GpsMile - firstWaypoint.GpsMile) / 1000.0, 2),
            DurationHours = Math.Round(durationSeconds / 3600.0, 2),
            AverageSpeedKmh = Math.Round(speeds.Average(), 2),
            MaxSpeedKmh = Math.Round(speeds.Max(), 2),
            StopCount = CountStops(orderedWaypoints),
            TotalWaypoints = waypoints.Count,
            MovingWaypoints = movingWaypoints.Count,
            StartLatitude = TruncateTo6Decimals(firstWaypoint.Y / 1000000.0),
            StartLongitude = TruncateTo6Decimals(firstWaypoint.X / 1000000.0),
            EndLatitude = TruncateTo6Decimals(lastWaypoint.Y / 1000000.0),
            EndLongitude = TruncateTo6Decimals(lastWaypoint.X / 1000000.0)
        };
    }

    /// <summary>
    /// Count the number of stops in a trip (simple count, not duration-based)
    /// </summary>
    private static int CountStops(List<Waypoint> orderedWaypoints)
    {
        const double STOP_SPEED_THRESHOLD = 100.0; // Speed stored as speed * 100 (100 = 1 km/h)

        var stops = 0;
        var inStop = false;

        foreach (var waypoint in orderedWaypoints)
        {
            if (waypoint.Speed < STOP_SPEED_THRESHOLD)
            {
                if (!inStop)
                {
                    stops++;
                    inStop = true;
                }
            }
            else
            {
                inStop = false;
            }
        }

        return stops;
    }

    /// <summary>
    /// Calculate running time, stop time, and number of stops from waypoints
    /// Minimum stop duration: 2 minutes (120 seconds) to count as a stop
    /// </summary>
    private (int runningTimeSeconds, int stopTimeSeconds, int stopCount) CalculateRunningAndStopTime(List<Waypoint> orderedWaypoints)
    {
        const int MIN_STOP_DURATION = 120; // Minimum 2 minutes to count as a stop

        if (orderedWaypoints.Count < 2)
        {
            return (0, 0, 0);
        }

        int runningTimeSeconds = 0;
        int stopTimeSeconds = 0;
        int stopCount = 0;
        bool inStop = false;
        int currentStopDuration = 0;

        for (int i = 1; i < orderedWaypoints.Count; i++)
        {
            var prevWaypoint = orderedWaypoints[i - 1];
            var currWaypoint = orderedWaypoints[i];
            var timeDiff = currWaypoint.GpsTime - prevWaypoint.GpsTime;
            var avgSpeed = (prevWaypoint.Speed + currWaypoint.Speed) / 2.0;

            // Use average speed to classify the time interval
            if (avgSpeed == 0) // Both waypoints stopped
            {
                stopTimeSeconds += timeDiff;

                if (!inStop)
                {
                    // Just entered a stop - start tracking duration
                    currentStopDuration = timeDiff;
                    inStop = true;
                }
                else
                {
                    // Already in a stop - accumulate duration
                    currentStopDuration += timeDiff;
                }
            }
            else // At least one waypoint moving
            {
                runningTimeSeconds += timeDiff;

                if (inStop)
                {
                    // Exiting a stop - count it if it was long enough
                    if (currentStopDuration >= MIN_STOP_DURATION)
                    {
                        stopCount++;
                    }
                    inStop = false;
                    currentStopDuration = 0;
                }
            }
        }

        // Handle case where the last waypoint is still in a stop
        if (inStop && currentStopDuration >= MIN_STOP_DURATION)
        {
            stopCount++;
        }

        return (runningTimeSeconds, stopTimeSeconds, stopCount);
    }

    /// <summary>
    /// Get vehicle history by license plate or custom plate (display name)
    /// </summary>
    public async Task<VehicleHistoryResult> GetVehicleHistoryByPlateAsync(
        string bearerToken,
        string plate,
        DateTime startTime,
        DateTime endTime)
    {
        var vehicle = await _vehicleService.GetVehicleByPlateAsync(bearerToken, plate);
        if (vehicle == null)
        {
            throw new InvalidOperationException($"No vehicle found with plate '{plate}'");
        }

        return await GetVehicleHistoryAsync(bearerToken, vehicle.Id, startTime, endTime);
    }
}

/// <summary>
/// Result model for vehicle history queries
/// </summary>
public class VehicleHistoryResult
{
    public string VehicleId { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalWaypoints { get; set; }
    public int MovingWaypoints { get; set; }
    public List<WaypointSummary> Waypoints { get; set; } = new();
    public int? HoursBack { get; set; }
    public string? Date { get; set; }
    
    // Trip Statistics
    public double TotalDistanceKm { get; set; }
    public double TotalRunningTimeHours { get; set; }
    public double TotalStopTimeHours { get; set; }
    public string TotalRunningTimeFormatted { get; set; } = "";
    public string TotalStopTimeFormatted { get; set; } = "";
    public int AmountOfTimeStop { get; set; }
    public double AverageSpeedKmh { get; set; }
    public double HighestSpeedKmh { get; set; }
}

/// <summary>
/// Summary of a single waypoint
/// </summary>
public class WaypointSummary
{
    public string Timestamp { get; set; } = "";
    public int RawGpsTime { get; set; }  // Raw GPS time value for testing purposes
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Altitude { get; set; }
    public double Speed { get; set; }
    public byte Heading { get; set; }
    public byte Satellites { get; set; }
    public double Mileage { get; set; }
    public double GpsMileage { get; set; }
    public double CumulativeDistanceKm { get; set; }  // Running total distance from start
    public short EventId { get; set; }
    public int Status { get; set; }
    public double Voltage { get; set; }
    public short Battery { get; set; }
    public string? DriverId { get; set; }
    public string? DriverCode { get; set; }
    public string? Info { get; set; }
}

/// <summary>
/// Result model for trip summary statistics
/// </summary>
public class VehicleTripSummary
{
    public string VehicleId { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public double TotalDistanceKm { get; set; }
    public double TotalGpsDistanceKm { get; set; }
    public double DurationHours { get; set; }
    public double AverageSpeedKmh { get; set; }
    public double MaxSpeedKmh { get; set; }
    public int StopCount { get; set; }
    public int TotalWaypoints { get; set; }
    public int MovingWaypoints { get; set; }
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }
}
