using McpVersionVer2.Models;

namespace McpVersionVer2.Services;

public class VehicleHistoryService
{
    private const double SPEED_DIVISOR = 100.0;
    private const double DISTANCE_DIVISOR = 1000.0;
    private const double GPS_COORDINATE_DIVISOR = 1_000_000.0;
    private const double VOLTAGE_DIVISOR = 1000.0;
    private const int MIN_STOP_DURATION_SECONDS = 120;
    private const int SHORT_IDLE_THRESHOLD_SECONDS = 120;
    private const int LONG_IDLE_THRESHOLD_SECONDS = 300;
    private const int MAX_HOURS_LOOKBACK = 168;
    private const double STOP_SPEED_THRESHOLD = 100.0;

    private readonly WaypointService _waypointService;
    private readonly VehicleService _vehicleService;

    private static readonly DateTime GpsEpoch = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Local);

    public VehicleHistoryService(
        WaypointService waypointService,
        VehicleService vehicleService)
    {
        _waypointService = waypointService;
        _vehicleService = vehicleService;
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
    private static List<Waypoint> GetFilteredAndSortedWaypoints(
        List<Waypoint> waypoints,
        int startGpsTime,
        int endGpsTime)
    {
        return waypoints
            .Where(w => w.GpsTime >= startGpsTime && w.GpsTime <= endGpsTime)
            .OrderBy(w => w.GpsTime)
            .ToList();
    }

    private static void ValidateInputs(string vehicleId, DateTime startTime, ref DateTime endTime)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
        {
            throw new ArgumentException("Vehicle ID cannot be empty or whitespace.", nameof(vehicleId));
        }

        if (startTime == endTime)
        {
            endTime = endTime.AddMinutes(1);
        }
        else if (startTime > endTime)
        {
            throw new ArgumentException("Start time must be before end time. Please provide a valid time range.");
        }
    }
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

    private List<WaypointSummary> ConvertToWaypointSummaries(List<Waypoint> waypoints)
    {
        var summaries = new List<WaypointSummary>();
        double cumulativeDistanceKm = 0.0;
        double consecutiveIdleSeconds = 0.0;
        int idleStartIndex = -1;

        for (int i = 0; i < waypoints.Count; i++)
        {
            var w = waypoints[i];
            var currentSpeed = w.Speed / SPEED_DIVISOR;

            if (i > 0)
            {
                var prev = waypoints[i - 1];
                var lat1 = prev.Y / GPS_COORDINATE_DIVISOR;
                var lon1 = prev.X / GPS_COORDINATE_DIVISOR;
                var lat2 = w.Y / GPS_COORDINATE_DIVISOR;
                var lon2 = w.X / GPS_COORDINATE_DIVISOR;

                int distanceInMeters = GisUtil.GetDistance(lon1, lat1, lon2, lat2);

                double distanceKm = distanceInMeters / DISTANCE_DIVISOR;
                cumulativeDistanceKm += distanceKm;

                double timeIntervalSeconds = w.GpsTime - prev.GpsTime;
                if (timeIntervalSeconds > 0)
                {
                    if (prev.Speed == 0)
                    {
                        consecutiveIdleSeconds += timeIntervalSeconds;
                    }
                    else
                    {
                        if (idleStartIndex >= 0 && consecutiveIdleSeconds <= SHORT_IDLE_THRESHOLD_SECONDS)
                        {
                            for (int j = idleStartIndex; j < i; j++)
                            {
                                summaries[j].VehicleStatus = "running";
                            }
                        }

                        consecutiveIdleSeconds = 0;
                        idleStartIndex = -1;
                    }
                }
            }

            string vehicleStatus;
            if (currentSpeed > 0)
            {
                vehicleStatus = "running";
                consecutiveIdleSeconds = 0;
                idleStartIndex = -1;
            }
            else
            {
                vehicleStatus = "idle";

                if (idleStartIndex == -1)
                {
                    idleStartIndex = summaries.Count;
                }

                if (consecutiveIdleSeconds > LONG_IDLE_THRESHOLD_SECONDS)
                {
                    vehicleStatus = "stop";
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
                Latitude = RoundTo6Decimals(w.Y / GPS_COORDINATE_DIVISOR),
                Longitude = RoundTo6Decimals(w.X / GPS_COORDINATE_DIVISOR),
                Altitude = w.Z,
                Speed = currentSpeed,
                Heading = w.Heading,
                Satellites = w.Satellite,
                Mileage = w.Mile / DISTANCE_DIVISOR,
                GpsMileage = w.GpsMile / DISTANCE_DIVISOR,
                CumulativeDistanceKm = RoundTo3Decimals(cumulativeDistanceKm),
                EventId = w.EventId,
                Status = w.Status,
                Voltage = w.Voltage / VOLTAGE_DIVISOR,
                Battery = w.Battery,
                DriverId = w.DriverId,
                DriverCode = w.DriverCode,
                Info = w.Info,
                VehicleStatus = vehicleStatus
            });
        }

        if (idleStartIndex >= 0 && summaries.Count > 0)
        {
            if (consecutiveIdleSeconds <= SHORT_IDLE_THRESHOLD_SECONDS)
            {
                for (int j = idleStartIndex; j < summaries.Count; j++)
                {
                    summaries[j].VehicleStatus = "running";
                }
            }
            else if (consecutiveIdleSeconds > LONG_IDLE_THRESHOLD_SECONDS)
            {
                for (int j = idleStartIndex; j < summaries.Count; j++)
                {
                    summaries[j].VehicleStatus = "stop";
                }
            }
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

        var startGpsTime = (int)(startTime - GpsEpoch).TotalSeconds;
        var endGpsTime = (int)(endTime - GpsEpoch).TotalSeconds;
        var filteredWaypoints = GetFilteredAndSortedWaypoints(waypoints, startGpsTime, endGpsTime);

        if (!filteredWaypoints.Any())
        {
            return CreateEmptyHistoryResult(vehicleId, startTime, endTime);
        }

        var waypointSummaries = ConvertToWaypointSummaries(filteredWaypoints);

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
        if (hours <= 0 || hours > MAX_HOURS_LOOKBACK)
        {
            throw new ArgumentException($"Hours must be between 1 and {MAX_HOURS_LOOKBACK} (1 week)");
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

        var startGpsTime = (int)(startTime - GpsEpoch).TotalSeconds;
        var endGpsTime = (int)(endTime - GpsEpoch).TotalSeconds;
        var filteredWaypoints = GetFilteredAndSortedWaypoints(waypoints, startGpsTime, endGpsTime);

        if (filteredWaypoints.Count < 2)
        {
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

        var firstWaypoint = filteredWaypoints.First();
        var lastWaypoint = filteredWaypoints.Last();

        var movingWaypointsFiltered = filteredWaypoints.Where(w => w.Speed > 0).ToList();

        if (!movingWaypointsFiltered.Any())
        {
            throw new InvalidOperationException($"No moving waypoints found for vehicle {vehicleId} in the specified time range.");
        }

        var waypointSummaries = ConvertToWaypointSummaries(filteredWaypoints);
        var totalGpsDistance = waypointSummaries.Last().CumulativeDistanceKm;

        var durationSeconds = (endTime - startTime).TotalSeconds;
        var allSpeeds = filteredWaypoints.Select(w => w.Speed / SPEED_DIVISOR).ToList();
        var movingSpeeds = movingWaypointsFiltered.Select(w => w.Speed / SPEED_DIVISOR).ToList();

        var (runningTimeSeconds, stopTimeSeconds, _) = CalculateRunningAndStopTime(filteredWaypoints);
        var amountOfTimeStop = Math.Round(stopTimeSeconds / 3600.0, 2); 
        var amountOfTimeRunning = Math.Round(runningTimeSeconds / 3600.0, 2);

        var (_, _, stopCountFromHistory) = CalculateRunningAndStopTime(filteredWaypoints);

        return new VehicleTripSummary
        {
            VehicleId = vehicleId,
            StartTime = ConvertGpsTimeToDateTime(firstWaypoint.GpsTime).ToString("dd-MM-yyyy HH:mm:ss"),
            EndTime = ConvertGpsTimeToDateTime(lastWaypoint.GpsTime).ToString("dd-MM-yyyy HH:mm:ss"),
            TotalDistanceKm = Math.Round(totalGpsDistance, 3),
            DurationHours = Math.Round(durationSeconds / 3600.0, 2),
            AverageSpeedKmh = Math.Round(allSpeeds.Average(), 2), 
            MaxSpeedKmh = Math.Round(movingSpeeds.Max(), 2), 
            StopCount = stopCountFromHistory, 
            TotalWaypoints = filteredWaypoints.Count,
            MovingWaypoints = movingWaypointsFiltered.Count,
            AmountOfTimeStop = amountOfTimeStop, 
            AmountOfTimeRunning = amountOfTimeRunning, 
            StartLatitude = RoundTo6Decimals(firstWaypoint.Y / GPS_COORDINATE_DIVISOR),
            StartLongitude = RoundTo6Decimals(firstWaypoint.X / GPS_COORDINATE_DIVISOR),
            StartInfo = firstWaypoint.Info ?? "",
            EndLatitude = RoundTo6Decimals(lastWaypoint.Y / GPS_COORDINATE_DIVISOR),
            EndLongitude = RoundTo6Decimals(lastWaypoint.X / GPS_COORDINATE_DIVISOR),
            EndInfo = lastWaypoint.Info ?? ""
        };
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

        double currentStopGroupDuration = 0;
        bool isStopping = false;

        for (int i = 0; i < orderedWaypoints.Count - 1; i++)
        {
            var current = orderedWaypoints[i];
            var next = orderedWaypoints[i + 1];

            double intervalSeconds = next.GpsTime - current.GpsTime;

            if (intervalSeconds <= 0)
            {
                continue;
            }

            if (current.Speed == 0)
            {
                isStopping = true;
                currentStopGroupDuration += intervalSeconds;
            }
            else
            {
                runningTimeSeconds += (int)intervalSeconds;

                if (isStopping)
                {
                    ProcessStopGroup(currentStopGroupDuration, ref runningTimeSeconds, ref stopTimeSeconds, ref stopCount);

                    currentStopGroupDuration = 0;
                    isStopping = false;
                }
            }
        }

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
            stopTotal += (int)duration;
            stopCount++;
        }
        else
        {
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