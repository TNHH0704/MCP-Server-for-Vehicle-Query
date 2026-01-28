namespace McpVersionVer2.Models.Dto;

/// <summary>
/// Daily statistics summary DTO
/// </summary>
public class DailyStatisticsSummaryDto
{
    public string Plate { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string GpsMileage { get; set; } = string.Empty;
    public string RunTime { get; set; } = string.Empty;
    public int EngineOffCount { get; set; }
    public int VehicleStopCount { get; set; }
    public int OverSpeed { get; set; }
    public string MaxSpeed { get; set; } = string.Empty;
    public string IdleTime { get; set; } = string.Empty;
    public string StopTime { get; set; } = string.Empty;
}