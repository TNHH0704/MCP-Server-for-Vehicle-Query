using System.Text.Json.Serialization;

namespace McpVersionVer2.Models.Dto;

/// <summary>
/// Daily statistics for a vehicle
/// </summary>
public class Daily
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