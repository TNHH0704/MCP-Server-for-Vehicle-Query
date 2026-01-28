using McpVersionVer2.Services;
using McpVersionVer2.Services.Mappers;

namespace McpVersionVer2.Models.Dto;

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

public static class VehicleStatusConstants
{
    public const double SPEED_DIVISOR = 100.0;
    public const double DISTANCE_DIVISOR = 1000.0;
    public const double VOLTAGE_DIVISOR = 100.0;
}

public class RealTimeSpeedDto
{
    public int CurrentSpeed { get; set; }
    public int MaxSpeed { get; set; }
    public double CurrentSpeedKmh => CurrentSpeed / VehicleStatusConstants.SPEED_DIVISOR;
    public double MaxSpeedKmh => MaxSpeed / VehicleStatusConstants.SPEED_DIVISOR;
    public bool IsOverSpeed => CurrentSpeed > MaxSpeed;
    public string CurrentSpeedFormatted => $"{CurrentSpeed / VehicleStatusConstants.SPEED_DIVISOR:F1} km/h";
    public string MaxSpeedFormatted => $"{MaxSpeed / VehicleStatusConstants.SPEED_DIVISOR:F1} km/h";
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
}

public class RealTimeDeviceDto
{
    public string Imei { get; set; } = string.Empty;
    public int DeviceTypeId { get; set; }
    public int Voltage { get; set; }
    public int Battery { get; set; }
    public string VoltageFormatted => $"{Voltage / VehicleStatusConstants.VOLTAGE_DIVISOR:F2}V";
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
    public string DistanceFormatted => $"{Distance / VehicleStatusConstants.DISTANCE_DIVISOR:F2} km";
    public string DurationFormatted => TimeSpan.FromSeconds(Duration).ToString(@"hh\:mm\:ss");
    public string MaxSpeedFormatted => $"{MaxSpeed / VehicleStatusConstants.SPEED_DIVISOR:F1} km/h";
}

public class RealTimeDailyStatsDto
{
    public int EngineOffCount { get; set; }
    public int VehicleStopCount { get; set; }
    public double TotalDistance { get; set; }
    public int RunTime { get; set; }
    public int IdleTime { get; set; }
    public int StopTime { get; set; }
    public int MaxSpeed { get; set; }
    public int OverSpeedCount { get; set; }
    public string TotalDistanceFormatted => $"{TotalDistance / VehicleStatusConstants.DISTANCE_DIVISOR:F2} km";
    public string RunTimeFormatted => TimeSpan.FromSeconds(RunTime).ToString(@"hh\:mm\:ss");
    public string IdleTimeFormatted => TimeSpan.FromSeconds(IdleTime).ToString(@"hh\:mm\:ss");
    public string StopTimeFormatted => TimeSpan.FromSeconds(StopTime).ToString(@"hh\:mm\:ss");
}

public class RealTimeTimestampsDto
{
    public DateTime GpsTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public DateTime? AccOffTime { get; set; }
    public string GpsTimeFormatted => GpsTime.ToString("dd-MM-yyyy HH:mm:ss");
    public string LastStopTimeFormatted => LastUpdateTime.ToString("dd-MM-yyyy HH:mm:ss");
}