using System.Text.Json.Serialization;

namespace McpVersionVer2.Models;

/// <summary>
/// Root response model for vehicle status API
/// </summary>
public class VehicleStatusResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("data")]
    public VehicleStatusData? Data { get; set; }
}

/// <summary>
/// Container for vehicle status data
/// </summary>
public class VehicleStatusData
{
    [JsonPropertyName("systemTime")]
    public long SystemTime { get; set; }

    [JsonPropertyName("currentTime")]
    public long CurrentTime { get; set; }

    [JsonPropertyName("data")]
    public List<VehicleStatus> Data { get; set; } = new();
}

/// <summary>
/// Detailed vehicle status information
/// </summary>
public class VehicleStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("vehicleGroup")]
    public VehicleGroupInfo? VehicleGroup { get; set; }

    [JsonPropertyName("vehicleTypeName")]
    public string VehicleTypeName { get; set; } = string.Empty;

    [JsonPropertyName("vehicleTypeId")]
    public int VehicleTypeId { get; set; }

    [JsonPropertyName("plate")]
    public string Plate { get; set; } = string.Empty;

    [JsonPropertyName("customPlateNumber")]
    public string CustomPlateNumber { get; set; } = string.Empty;

    [JsonPropertyName("maxSpeed")]
    public int MaxSpeed { get; set; }

    [JsonPropertyName("gpsTime")]
    public long GpsTime { get; set; }

    [JsonPropertyName("speed")]
    public int Speed { get; set; }

    [JsonPropertyName("regionId")]
    public int RegionId { get; set; }

    [JsonPropertyName("x")]
    public long X { get; set; }

    [JsonPropertyName("y")]
    public long Y { get; set; }

    [JsonPropertyName("z")]
    public int Z { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("info")]
    public string Info { get; set; } = string.Empty;

    [JsonPropertyName("heading")]
    public int Heading { get; set; }

    [JsonPropertyName("satellite")]
    public int Satellite { get; set; }

    [JsonPropertyName("gpsMileage")]
    public int GpsMileage { get; set; }

    [JsonPropertyName("stopOrIdleTime")]
    public long StopOrIdleTime { get; set; }

    [JsonPropertyName("trip")]
    public TripInfo? Trip { get; set; }

    [JsonPropertyName("daily")]
    public DailyStatistics? Daily { get; set; }

    [JsonPropertyName("totalMileage")]
    public int TotalMileage { get; set; }

    [JsonPropertyName("input")]
    public int Input { get; set; }

    [JsonPropertyName("inputs")]
    public InputStatus? Inputs { get; set; }

    [JsonPropertyName("output")]
    public int Output { get; set; }

    [JsonPropertyName("lastUpdateTime")]
    public long LastUpdateTime { get; set; }

    [JsonPropertyName("road")]
    public string Road { get; set; } = string.Empty;

    [JsonPropertyName("deviceTypeId")]
    public int DeviceTypeId { get; set; }

    [JsonPropertyName("imei")]
    public string Imei { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public int Icon { get; set; }

    [JsonPropertyName("idleTime")]
    public long IdleTime { get; set; }

    [JsonPropertyName("accOffTime")]
    public long AccOffTime { get; set; }

    [JsonPropertyName("sleepTime")]
    public long SleepTime { get; set; }

    [JsonPropertyName("geometry")]
    public object? Geometry { get; set; }

    [JsonPropertyName("customKeys")]
    public List<object> CustomKeys { get; set; } = new();

    [JsonPropertyName("customParams")]
    public object? CustomParams { get; set; }

    [JsonPropertyName("params")]
    public string Params { get; set; } = string.Empty;

    [JsonPropertyName("voltage")]
    public int Voltage { get; set; }

    [JsonPropertyName("battery")]
    public int Battery { get; set; }
}

/// <summary>
/// Vehicle group information
/// </summary>
public class VehicleGroupInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Trip information for a vehicle
/// </summary>
public class TripInfo
{
    [JsonPropertyName("fromTime")]
    public long FromTime { get; set; }

    [JsonPropertyName("toTime")]
    public long ToTime { get; set; }

    [JsonPropertyName("gpsMileage")]
    public int GpsMileage { get; set; }

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("maxSpeed")]
    public int MaxSpeed { get; set; }

    [JsonPropertyName("minSpeed")]
    public int MinSpeed { get; set; }

    [JsonPropertyName("averageSpeed")]
    public int AverageSpeed { get; set; }

    [JsonPropertyName("waypoint")]
    public int Waypoint { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

/// <summary>
/// Daily statistics for a vehicle
/// </summary>
public class DailyStatistics
{
    [JsonPropertyName("stopCount")]
    public int StopCount { get; set; }

    [JsonPropertyName("idleCount")]
    public int IdleCount { get; set; }

    [JsonPropertyName("doorCloseCount")]
    public int DoorCloseCount { get; set; }

    [JsonPropertyName("doorOpenCount")]
    public int DoorOpenCount { get; set; }

    [JsonPropertyName("gpsMileage")]
    public int GpsMileage { get; set; }

    [JsonPropertyName("runTime")]
    public int RunTime { get; set; }

    [JsonPropertyName("overSpeed")]
    public int OverSpeed { get; set; }

    [JsonPropertyName("accTime")]
    public int AccTime { get; set; }

    [JsonPropertyName("maxSpeed")]
    public int MaxSpeed { get; set; }

    [JsonPropertyName("idleTime")]
    public int IdleTime { get; set; }

    [JsonPropertyName("stopTime")]
    public int StopTime { get; set; }
}

/// <summary>
/// Input status for vehicle sensors
/// </summary>
public class InputStatus
{
    [JsonPropertyName("input1")]
    public int Input1 { get; set; }

    [JsonPropertyName("input2")]
    public int Input2 { get; set; }

    [JsonPropertyName("input3")]
    public int Input3 { get; set; }

    [JsonPropertyName("input4")]
    public int Input4 { get; set; }
}

/// <summary>
/// Request model for vehicle status API
/// </summary>
public class VehicleStatusRequest
{
    [JsonPropertyName("time")]
    public long Time { get; set; }
}
