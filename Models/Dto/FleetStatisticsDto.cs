namespace McpVersionVer2.Models.Dto;

/// <summary>
/// Fleet statistics DTO for aggregated data
/// </summary>
public class FleetStatisticsDto
{
    public int TotalVehicles { get; set; }
    public int ActiveVehicles { get; set; }
    public int InactiveVehicles { get; set; }
    public Dictionary<string, int> VehiclesByStatus { get; set; } = new();
    public Dictionary<string, int> VehiclesByCompany { get; set; } = new();
    public Dictionary<string, int> VehiclesByType { get; set; } = new();
    public int VehiclesOverSpeed { get; set; }
    public int VehiclesWithExpiredInsurance { get; set; }
    public int VehiclesWithExpiredRegistry { get; set; }
    public DateTime LastUpdated { get; set; }
}