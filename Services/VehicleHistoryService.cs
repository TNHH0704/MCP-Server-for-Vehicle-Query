using Microsoft.Extensions.Logging;

namespace McpVersionVer2.Services;

public class VehicleHistoryService
{
    // Conversion constants
    private const double SPEED_DIVISOR = 100.0;
    private const double DISTANCE_DIVISOR = 1000.0;
    private const double GPS_COORDINATE_DIVISOR = 1_000_000.0;
    private const int MIN_STOP_DURATION_SECONDS = 120;
    private const double STOP_SPEED_THRESHOLD = 100.0; // Speed * 100 (100 = 1 km/h)

    private readonly WaypointService _waypointService;
    private readonly VehicleService _vehicleService;
    private readonly ILogger<VehicleHistoryService> _logger;

    // GPS tracking systems use 2010-01-01 as epoch (not Unix epoch 1970-01-01)
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
    /// Round a number to 3 decimal places
    /// </summary>
    private static double RoundTo3Decimals(double value)
    {
        return Math.Round(value, 3);
    }



    /// <summary>
    /// Round a number to 6 decimal places
    /// </summary>
    private static double RoundTo6Decimals(double value)
    {
        return Math.Round(value, 6);
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
            TotalRunningTime = "00:00:00",
            TotalStopTime = "00:00:00",
            AmountOfTimeStop = 0,
            AverageSpeedKmh = 0,
            HighestSpeedKmh = 0
        };
    }

    /// <summary>
    /// Convert waypoints to summary format with proper unit conversions
    /// Includes cumulative distance calculation and vehicle status determination
    /// </summary>
    private List<WaypointSummary> ConvertToWaypointSummaries(List<Waypoint> waypoints)
    {
        var summaries = new List<WaypointSummary>();
        double cumulativeDistanceKm = 0.0;
        double consecutiveIdleSeconds = 0.0; // Track consecutive idle time
        int idleStartIndex = -1; // Index where current idle period started

        for (int i = 0; i < waypoints.Count; i++)
        {
            var w = waypoints[i];
            var currentSpeed = w.Speed / SPEED_DIVISOR;

            // Calculate distance from previous waypoint
            if (i > 0)
            {
                var prev = waypoints[i - 1];
                var lat1 = prev.Y / GPS_COORDINATE_DIVISOR;
                var lon1 = prev.X / GPS_COORDINATE_DIVISOR;
                var lat2 = w.Y / GPS_COORDINATE_DIVISOR;
                var lon2 = w.X / GPS_COORDINATE_DIVISOR;

                // Use GisUtil.GetDistance directly (returns meters as int, following its rounding)
                int distanceInMeters = GisUtil.GetDistance(lon1, lat1, lon2, lat2);
                
                // Convert to kilometers and accumulate
                double distanceKm = distanceInMeters / 1000.0;
                cumulativeDistanceKm += distanceKm;

                // Calculate time interval for status determination
                double timeIntervalSeconds = w.GpsTime - prev.GpsTime;
                if (timeIntervalSeconds > 0)
                {
                    if (prev.Speed == 0)  // Check if PREVIOUS waypoint had speed = 0
                    {
                        // Accumulate consecutive idle time
                        consecutiveIdleSeconds += timeIntervalSeconds;
                    }
                    else
                    {
                        // Speed > 0, check if we need to retroactively change idle period to running
                        if (idleStartIndex >= 0 && consecutiveIdleSeconds <= 120) // 2 minutes
                        {
                            // Change all waypoints in the idle period from idleStartIndex to i-1 to "running"
                            for (int j = idleStartIndex; j < i; j++)
                            {
                                summaries[j].VehicleStatus = "running";
                            }
                        }
                        
                        // Reset idle tracking
                        consecutiveIdleSeconds = 0;
                        idleStartIndex = -1;
                    }
                }
            }

            // Determine initial vehicle status
            string vehicleStatus;
            if (currentSpeed > 0)
            {
                vehicleStatus = "running";
                // Reset idle tracking when speed becomes positive
                consecutiveIdleSeconds = 0;
                idleStartIndex = -1;
            }
            else
            {
                // Speed is 0, immediately set to idle
                vehicleStatus = "idle";
                
                // Start tracking idle period if not already tracking
                if (idleStartIndex == -1)
                {
                    idleStartIndex = summaries.Count; // This will be the index of current waypoint
                }
                
                // Check if idle time exceeds 5 minutes
                if (consecutiveIdleSeconds > 300) // 5 minutes
                {
                    vehicleStatus = "stop";
                    // Change all previous idle waypoints in this period to "stop"
                    if (idleStartIndex >= 0)
                    {
                        for (int j = idleStartIndex; j < summaries.Count; j++)
                        {
                            summaries[j].VehicleStatus = "stop";
                        }
                    }
                }
            }

            summaries.Add(new WaypointSummary
            {
                Timestamp = ConvertGpsTimeToDateTime(w.GpsTime).ToString("dd-MM-yyyy HH:mm:ss"),
                RawGpsTime = w.GpsTime,
                Latitude = RoundTo6Decimals(w.Y / 1000000.0),
                Longitude = RoundTo6Decimals(w.X / 1000000.0),
                Altitude = w.Z,
                Speed = currentSpeed,
                Heading = w.Heading,
                Satellites = w.Satellite,
                Mileage = w.Mile / DISTANCE_DIVISOR,
                GpsMileage = w.GpsMile / DISTANCE_DIVISOR,
                CumulativeDistanceKm = RoundTo3Decimals(cumulativeDistanceKm),
                EventId = w.EventId,
                Status = w.Status,
                Voltage = w.Voltage / 1000.0,
                Battery = w.Battery,
                DriverId = w.DriverId,
                DriverCode = w.DriverCode,
                Info = w.Info,
                VehicleStatus = vehicleStatus
            });
        }

        // Handle case where file ends with idle period
        if (idleStartIndex >= 0 && summaries.Count > 0)
        {
            if (consecutiveIdleSeconds <= 120) // 2 minutes
            {
                // If the last idle period was <= 2 minutes, change it to running
                for (int j = idleStartIndex; j < summaries.Count; j++)
                {
                    summaries[j].VehicleStatus = "running";
                }
            }
            else if (consecutiveIdleSeconds > 300) // 5 minutes
            {
                // If the last idle period was > 5 minutes, change it to stop
                for (int j = idleStartIndex; j < summaries.Count; j++)
                {
                    summaries[j].VehicleStatus = "stop";
                }
            }
            // If between 2-5 minutes, leave as idle
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
        var totalSpeed = sortedWaypoints.Sum(w => w.Speed / SPEED_DIVISOR);
        var avgSpeed = sortedWaypoints.Count > 0 ? totalSpeed / sortedWaypoints.Count : 0;
        var maxSpeed = sortedWaypoints.Any() ? sortedWaypoints.Max(w => w.Speed / SPEED_DIVISOR) : 0;

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

        // Ensure waypoints are within the requested time range
        var startGpsTime = (int)(startTime - GpsEpoch).TotalSeconds;
        var endGpsTime = (int)(endTime - GpsEpoch).TotalSeconds;
        var filteredWaypoints = sortedWaypoints
            .Where(w => w.GpsTime >= startGpsTime && w.GpsTime <= endGpsTime)
            .ToList();

        if (!filteredWaypoints.Any())
        {
            return CreateEmptyHistoryResult(vehicleId, startTime, endTime);
        }

        var waypointSummaries = ConvertToWaypointSummaries(filteredWaypoints);

        // Get total distance from the last waypoint's cumulative distance (already truncated)
        var totalDistanceKm = waypointSummaries.Any() ? waypointSummaries.Last().CumulativeDistanceKm : 0.0;

        var statistics = CalculateTripStatistics(filteredWaypoints, totalDistanceKm);

        return new VehicleHistoryResult
        {
            VehicleId = vehicleId,
            StartTime = startTime,
            EndTime = endTime,
            TotalWaypoints = filteredWaypoints.Count,
            MovingWaypoints = statistics.MovingCount,
            Waypoints = waypointSummaries,
            TotalDistanceKm = totalDistanceKm,
            TotalRunningTime = TimeSpan.FromHours(statistics.TotalRunningTimeHours).ToString(@"hh\:mm\:ss"),
            TotalStopTime = TimeSpan.FromHours(statistics.TotalStopTimeHours).ToString(@"hh\:mm\:ss"),
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
        result.Date = date.ToString("dd-MM-yyyy");
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

        // Use all waypoints for distance calculation (like ConvertToWaypointSummaries does)
        var orderedWaypoints = waypoints.OrderBy(w => w.GpsTime).ToList();
        var firstWaypoint = orderedWaypoints.First();
        var lastWaypoint = orderedWaypoints.Last();

        // Ensure waypoints are within the requested time range and properly ordered
        var startGpsTime = (int)(startTime - GpsEpoch).TotalSeconds;
        var endGpsTime = (int)(endTime - GpsEpoch).TotalSeconds;
        var filteredWaypoints = orderedWaypoints
            .Where(w => w.GpsTime >= startGpsTime && w.GpsTime <= endGpsTime)
            .OrderBy(w => w.GpsTime)
            .ToList();

        if (filteredWaypoints.Count < 2)
        {
            // Not enough waypoints to calculate running/stop time
            return new VehicleTripSummary
            {
                VehicleId = vehicleId,
                StartTime = startTime.ToString("dd-MM-yyyy HH:mm:ss"),
                EndTime = endTime.ToString("dd-MM-yyyy HH:mm:ss"),
                TotalDistanceKm = 0,
                DurationHours = Math.Round((endTime - startTime).TotalSeconds / 3600.0, 2),
                AverageSpeedKmh = 0,
                MaxSpeedKmh = 0,
                StopCount = 0,
                TotalWaypoints = waypoints.Count,
                MovingWaypoints = 0,
                AmountOfTimeStop = 0,
                AmountOfTimeRunning = 0,
                StartLatitude = 0,
                StartLongitude = 0,
                StartInfo = "",
                EndLatitude = 0,
                EndLongitude = 0,
                EndInfo = ""
            };
        }

        var movingWaypointsFiltered = filteredWaypoints.Where(w => w.Speed > 0).ToList();

        if (!movingWaypointsFiltered.Any())
        {
            throw new InvalidOperationException($"No moving waypoints found for vehicle {vehicleId} in the specified time range.");
        }

        // Update first/last waypoints from filtered list
        firstWaypoint = filteredWaypoints.First();
        lastWaypoint = filteredWaypoints.Last();

        // Calculate total distance using GPS-based cumulative distance from filtered waypoints
        var waypointSummaries = ConvertToWaypointSummaries(filteredWaypoints);
        var totalGpsDistance = waypointSummaries.Last().CumulativeDistanceKm;

        // Calculate duration as the requested time range
        var durationSeconds = (endTime - startTime).TotalSeconds;
        // Use filtered waypoints for average speed calculation
        var allSpeeds = filteredWaypoints.Select(w => w.Speed / SPEED_DIVISOR).ToList();
        // Keep moving waypoints for max speed (only consider periods of movement)
        var movingSpeeds = movingWaypointsFiltered.Select(w => w.Speed / SPEED_DIVISOR).ToList();

        // Calculate stop time using the filtered waypoints
        var (runningTimeSeconds, stopTimeSeconds, _) = CalculateRunningAndStopTime(filteredWaypoints);
        var amountOfTimeStop = Math.Round(stopTimeSeconds / 3600.0, 2); // Convert to hours
        var amountOfTimeRunning = Math.Round(runningTimeSeconds / 3600.0, 2); // Convert to hours

        // Use the same stop counting logic as GetVehicleHistory (minimum 2 minutes)
        var (_, _, stopCountFromHistory) = CalculateRunningAndStopTime(filteredWaypoints);

        return new VehicleTripSummary
        {
            VehicleId = vehicleId,
            StartTime = ConvertGpsTimeToDateTime(firstWaypoint.GpsTime).ToString("dd-MM-yyyy HH:mm:ss"),
            EndTime = ConvertGpsTimeToDateTime(lastWaypoint.GpsTime).ToString("dd-MM-yyyy HH:mm:ss"),
            TotalDistanceKm = Math.Round(totalGpsDistance, 3),
            DurationHours = Math.Round(durationSeconds / 3600.0, 2),
            AverageSpeedKmh = Math.Round(allSpeeds.Average(), 2), // Use all waypoints for average speed
            MaxSpeedKmh = Math.Round(movingSpeeds.Max(), 2), // Use moving waypoints for max speed
            StopCount = stopCountFromHistory, // Use same stop counting logic as GetVehicleHistory
            TotalWaypoints = filteredWaypoints.Count,
            MovingWaypoints = movingWaypointsFiltered.Count,
            AmountOfTimeStop = amountOfTimeStop, // Add stop time in hours
            AmountOfTimeRunning = amountOfTimeRunning, // Add running time in hours
            StartLatitude = RoundTo6Decimals(firstWaypoint.Y / GPS_COORDINATE_DIVISOR),
            StartLongitude = RoundTo6Decimals(firstWaypoint.X / GPS_COORDINATE_DIVISOR),
            StartInfo = firstWaypoint.Info ?? "",
            EndLatitude = RoundTo6Decimals(lastWaypoint.Y / GPS_COORDINATE_DIVISOR),
            EndLongitude = RoundTo6Decimals(lastWaypoint.X / GPS_COORDINATE_DIVISOR),
            EndInfo = lastWaypoint.Info ?? ""
        };
    }

    /// <summary>
    /// Count the number of stops in a trip (simple count, not duration-based)
    /// </summary>
    private static int CountStops(List<Waypoint> orderedWaypoints)
    {

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
    /// Running Time = moving intervals + short stop groups (≤120s)
    /// Stop Time = long stop groups (>120s)
    /// Stop Count = number of long stop groups (>120s)
    /// </summary>
    private (int runningTimeSeconds, int stopTimeSeconds, int stopCount) CalculateRunningAndStopTime(List<Waypoint> orderedWaypoints)
    {
        if (orderedWaypoints.Count < 2)
        {
            return (0, 0, 0);
        }

        int runningTimeSeconds = 0;
        int stopTimeSeconds = 0;
        int stopCount = 0;

        // Variables to track consecutive stop groups
        double currentStopGroupDuration = 0;
        bool isStopping = false;

        for (int i = 0; i < orderedWaypoints.Count - 1; i++)
        {
            var current = orderedWaypoints[i];
            var next = orderedWaypoints[i + 1];

            // Calculate duration of this specific interval
            double intervalSeconds = next.GpsTime - current.GpsTime;

            // Skip intervals with no time difference (duplicate timestamps)
            if (intervalSeconds <= 0)
            {
                continue;
            }

            // Check if current waypoint indicates stopping (speed = 0)
            if (current.Speed == 0)
            {
                // Accumulate duration for the current stop group
                isStopping = true;
                currentStopGroupDuration += intervalSeconds;
            }
            else
            {
                // Speed > 0, so this is definitely moving time
                runningTimeSeconds += (int)intervalSeconds;

                // If we were previously tracking a stop group, we need to finalize it now
                if (isStopping)
                {
                    ProcessStopGroup(currentStopGroupDuration, ref runningTimeSeconds, ref stopTimeSeconds, ref stopCount);

                    // Reset stop tracking
                    currentStopGroupDuration = 0;
                    isStopping = false;
                }
            }
        }

        // Handle the final group if the file ends while stopped
        if (isStopping)
        {
            ProcessStopGroup(currentStopGroupDuration, ref runningTimeSeconds, ref stopTimeSeconds, ref stopCount);
        }

        return (runningTimeSeconds, stopTimeSeconds, stopCount);
    }

    private static void ProcessStopGroup(double duration, ref int runTotal, ref int stopTotal, ref int stopCount)
    {
        if (duration > MIN_STOP_DURATION_SECONDS)
        {
            // Long stop -> Actual Stop Time
            stopTotal += (int)duration;
            stopCount++;
        }
        else
        {
            // Short stop (traffic light, etc.) -> Add to Running Time
            runTotal += (int)duration;
        }
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
    public string TotalRunningTime { get; set; } = "";
    public string TotalStopTime { get; set; } = "";
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
    public string VehicleStatus { get; set; } = ""; // "running", "idle", or "stop"
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
    public double DurationHours { get; set; }
    public double AverageSpeedKmh { get; set; }
    public double MaxSpeedKmh { get; set; }
    public int StopCount { get; set; }
    public int TotalWaypoints { get; set; }
    public int MovingWaypoints { get; set; }
    public double AmountOfTimeStop { get; set; } // Time spent stopped in hours
    public double AmountOfTimeRunning { get; set; } // Time spent running in hours
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public string StartInfo { get; set; } = "";
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }
    public string EndInfo { get; set; } = "";
}
