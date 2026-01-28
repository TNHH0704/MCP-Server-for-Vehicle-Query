using McpVersionVer2.Services;
using McpVersionVer2.Services.Mappers;

namespace McpVersionVer2.Models.Dto;

/// <summary>
/// DTO for presenting vehicle data to MCP tools with processed/enriched information
/// </summary>
public class VehicleDto
{
    public string Id { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string VehicleGroup { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public CompanyInfo Company { get; set; } = new();

    public DeviceInfo Device { get; set; } = new();
    public VehicleStatusDto Status { get; set; } = new();

    public LocationDto Location { get; set; } = new();

    public DateInfoDto Dates { get; set; } = new();

    public VehicleFlagsDto Flags { get; set; } = new();

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
    public int CurrentSpeed { get; set; }
    public int MaxSpeed { get; set; }
    public bool IsOverSpeed { get; set; }
    public string SpeedDescription { get; set; } = string.Empty;
}

/// <summary>
/// Processed location information with human-readable coordinates
/// </summary>
public class LocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string FormattedCoordinates { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime? GpsTimestamp { get; set; }
    public string GpsColor { get; set; } = string.Empty;
    public bool HasValidGps { get; set; }
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