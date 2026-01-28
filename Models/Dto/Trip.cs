using System.Text.Json.Serialization;

namespace McpVersionVer2.Models.Dto;

/// <summary>
/// Trip information for a vehicle
/// </summary>
public class Trip
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