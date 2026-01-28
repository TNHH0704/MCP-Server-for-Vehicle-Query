namespace McpVersionVer2.Models.Dto;

/// <summary>
/// Simplified vehicle summary for quick overview
/// </summary>
public class VehicleSummaryDto
{
    public string Plate { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Speed { get; set; }
    public string Company { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string LastUpdated { get; set; } = string.Empty;
}