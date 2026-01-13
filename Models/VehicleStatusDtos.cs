namespace McpVersionVer2.Models;

/// <summary>
/// DTO for real-time vehicle status display
/// </summary>
public class RealTimeVehicleStatusDto
{
    public string Id { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public RealTimeVehicleGroupDto Group { get; set; } = new();
    public string VehicleType { get; set; } = string.Empty;
    public RealTimeLocationDto Location { get; set; } = new();
    public RealTimeSpeedDto Speed { get; set; } = new();
    public RealTimeStatusDto Status { get; set; } = new();
    public RealTimeDeviceDto Device { get; set; } = new();
    public RealTimeTripDto? CurrentTrip { get; set; }
    public RealTimeDailyStatsDto DailyStats { get; set; } = new();
    public RealTimeTimestampsDto Timestamps { get; set; } = new();
}

public class RealTimeVehicleGroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class RealTimeLocationDto
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int Altitude { get; init; }
    public string Address { get; init; } = string.Empty;
    public int Heading { get; init; }
    public int SatelliteCount { get; init; }
}

public class RealTimeSpeedDto
{
    public int CurrentSpeed { get; set; }  // Raw value (multiply by 100)
    public int MaxSpeed { get; set; }  // Raw value (multiply by 100)
    public double CurrentSpeedKmh => CurrentSpeed / 100.0;  // Actual speed in km/h
    public double MaxSpeedKmh => MaxSpeed / 100.0;  // Actual max speed in km/h
    public bool IsOverSpeed => CurrentSpeed > MaxSpeed;
    public string CurrentSpeedFormatted => $"{CurrentSpeed / 100.0:F1} km/h";
    public string MaxSpeedFormatted => $"{MaxSpeed / 100.0:F1} km/h";
}

public class RealTimeStatusDto
{
    public int StatusCode { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public long StopOrIdleTime { get; set; }
    public int Input { get; set; }
    public bool IsMoving => StatusCode == 2;
    public bool IsStopped => StatusCode == 0;
    public bool IsIdle => StatusCode == 1;
    public bool IsRunning { get; set; }
    public bool IsInReverse { get; set; }
}

public class RealTimeDeviceDto
{
    public string Imei { get; set; } = string.Empty;
    public int DeviceTypeId { get; set; }
    public int Voltage { get; set; }
    public int Battery { get; set; }
    public string VoltageFormatted => $"{Voltage / 100.0:F2}V";
}

public class RealTimeTripDto
{
    public DateTime FromTime { get; set; }
    public DateTime ToTime { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public double Distance { get; set; }
    public int Duration { get; set; }
    public int MaxSpeed { get; set; }
    public int AverageSpeed { get; set; }
    public string DistanceFormatted => $"{Distance / 1000.0:F2} km";
    public string DurationFormatted => TimeSpan.FromSeconds(Duration).ToString(@"hh\:mm\:ss");
    public string MaxSpeedFormatted => $"{MaxSpeed / 100.0:F1} km/h";
}

public class RealTimeDailyStatsDto
{
    public int StopCount { get; set; }
    public int IdleCount { get; set; }
    public double TotalDistance { get; set; }
    public int RunTime { get; set; }
    public int IdleTime { get; set; }
    public int StopTime { get; set; }
    public int MaxSpeed { get; set; }
    public int OverSpeedCount { get; set; }
    public string TotalDistanceFormatted => $"{TotalDistance / 1000.0:F2} km";
    public string RunTimeFormatted => TimeSpan.FromSeconds(RunTime).ToString(@"hh\:mm\:ss");
    public string IdleTimeFormatted => TimeSpan.FromSeconds(IdleTime).ToString(@"hh\:mm\:ss");
    public string StopTimeFormatted => TimeSpan.FromSeconds(StopTime).ToString(@"hh\:mm\:ss");
}

public class RealTimeTimestampsDto
{
    public DateTime GpsTime { get; set; }  // Vehicle status last update time
    public DateTime LastUpdateTime { get; set; }  // Last stop time
    public DateTime? AccOffTime { get; set; }
    public string GpsTimeFormatted => GpsTime.ToString("dd-MM-yyyy HH:mm:ss");
    public string LastStopTimeFormatted => LastUpdateTime.ToString("dd-MM-yyyy HH:mm:ss");
}

/// <summary>
/// Summary DTO for real-time vehicle status list
/// </summary>
public class RealTimeVehicleStatusSummaryDto
{
    public string Plate { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Speed { get; set; } = string.Empty;
    public string MaxSpeed { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LastUpdate { get; set; } = string.Empty;
    public string LastStopTime { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public bool IsInReverse { get; set; }
}

/// <summary>
/// Search result wrapper for real-time vehicle status
/// </summary>
public class RealTimeVehicleStatusSearchResult
{
    public int TotalCount { get; set; }
    public string SearchCriteria { get; set; } = string.Empty;
    public List<RealTimeVehicleStatusSummaryDto> Vehicles { get; set; } = new();
}

/// <summary>
/// Daily statistics summary DTO
/// </summary>
public class DailyStatisticsSummaryDto
{
    public string Plate { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string GpsMileage { get; set; } = string.Empty;
    public string RunTime { get; set; } = string.Empty;
    public int StopCount { get; set; }
    public int IdleCount { get; set; }
    public int OverSpeed { get; set; }
    public string MaxSpeed { get; set; } = string.Empty;
    public string IdleTime { get; set; } = string.Empty;
    public string StopTime { get; set; } = string.Empty;
}
