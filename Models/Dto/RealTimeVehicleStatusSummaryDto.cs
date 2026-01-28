namespace McpVersionVer2.Models.Dto;

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
}