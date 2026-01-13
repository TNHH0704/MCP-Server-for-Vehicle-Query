namespace McpVersionVer2.Models;

/// <summary>
/// DTO for presenting vehicle data to MCP tools with processed/enriched information
/// </summary>
public class VehicleDto
{
    // Basic Vehicle Information
    public string Id { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty; // Processed custom plate
    public string VehicleType { get; set; } = string.Empty;
    public string VehicleGroup { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Company Information
    public CompanyInfo Company { get; set; } = new();

    // Device Information
    public DeviceInfo Device { get; set; } = new();

    // Current Status
    public VehicleStatusDto Status { get; set; } = new();

    // Location Information
    public LocationDto Location { get; set; } = new();

    // Dates and Timestamps
    public DateInfoDto Dates { get; set; } = new();

    // Flags
    public VehicleFlagsDto Flags { get; set; } = new();

    // Metadata
    public string LastUpdatedBy { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Company-related information
/// </summary>
public class CompanyInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Device and tracking information
/// </summary>
public class DeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Imei { get; set; } = string.Empty;
    public string SimPhone { get; set; } = string.Empty;
    public string Iccid { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
}

/// <summary>
/// Processed vehicle status information
/// </summary>
public class VehicleStatusDto
{
    public string StatusName { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int CurrentSpeed { get; set; } // km/h
    public int MaxSpeed { get; set; } // km/h
    public bool IsOverSpeed { get; set; } // Calculated flag
    public string SpeedDescription { get; set; } = string.Empty; // e.g., "Stopped", "Moving normally", "Over speed!"
}

/// <summary>
/// Processed location information with human-readable coordinates
/// </summary>
public class LocationDto
{
    public double Latitude { get; set; } // Processed from Y
    public double Longitude { get; set; } // Processed from X
    public string FormattedCoordinates { get; set; } = string.Empty; // e.g., "10.760351, 106.674400"
    public string Address { get; set; } = string.Empty; // Address from Info field
    public DateTime? GpsTimestamp { get; set; } // Converted from Unix timestamp
    public string GpsColor { get; set; } = string.Empty;
    public bool HasValidGps { get; set; } // Calculated flag
}

/// <summary>
/// Date and expiration information with calculated flags
/// </summary>
public class DateInfoDto
{
    public DateTime? ActiveDate { get; set; }
    public DateTime? InsuranceDate { get; set; }
    public DateTime? InsuranceExpiration { get; set; }
    public DateTime? RegistryDate { get; set; }
    public DateTime? RegistryExpiration { get; set; }
    
    // Calculated flags
    public bool IsInsuranceExpired { get; set; }
    public bool IsRegistryExpired { get; set; }
    public int? DaysUntilInsuranceExpires { get; set; }
    public int? DaysUntilRegistryExpires { get; set; }
}

/// <summary>
/// Vehicle status flags
/// </summary>
public class VehicleFlagsDto
{
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public bool IsAssigned { get; set; }
}

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

/// <summary>
/// DTO for vehicle search results
/// </summary>
public class VehicleSearchResultDto
{
    public int TotalCount { get; set; }
    public List<VehicleSummaryDto> Vehicles { get; set; } = new();
    public string SearchCriteria { get; set; } = string.Empty;
}
