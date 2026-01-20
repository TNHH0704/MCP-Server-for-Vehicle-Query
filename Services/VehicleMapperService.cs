using McpVersionVer2.Models;

namespace McpVersionVer2.Services;

/// <summary>
/// Service for mapping raw vehicle data to processed DTOs
/// </summary>
public class VehicleMapperService
{
    // Conversion constants
    private const double SPEED_DIVISOR = 100.0;
    private const double DISTANCE_DIVISOR = 1000.0;
    private const double GPS_COORDINATE_DIVISOR = 1_000_000.0;

    /// <summary>
    /// Maps a raw VehicleResponse to a processed VehicleDto
    /// </summary>
    public VehicleDto MapToDto(VehicleResponse vehicle)
    {
        var dto = new VehicleDto
        {
            Id = vehicle.Id,
            Plate = vehicle.Plate,
            DisplayName = ProcessDisplayName(vehicle.CustomPlateNumber, vehicle.Plate),
            VehicleType = vehicle.VehicleTypeName,
            VehicleGroup = vehicle.VehicleGroupName,
            Description = vehicle.Description,
            LastUpdatedBy = vehicle.UpdatedByName,
            LastUpdated = vehicle.UpdatedAt,

            Company = new CompanyInfo
            {
                Id = vehicle.CompanyId,
                Name = vehicle.CompanyName,
                Phone = vehicle.CompanyPhone
            },

            Device = new DeviceInfo
            {
                Id = vehicle.DeviceId,
                Type = vehicle.DeviceTypeName,
                Imei = vehicle.Imei,
                SimPhone = vehicle.SimPhone,
                Iccid = vehicle.Iccid,
                FirmwareVersion = vehicle.FirmwareVersion
            },

            Status = MapStatus(vehicle),
            Location = MapLocation(vehicle.RawStatus),
            Dates = MapDates(vehicle),

            Flags = new VehicleFlagsDto
            {
                IsActive = vehicle.IsActive,
                IsLocked = vehicle.IsLocked,
                IsAssigned = vehicle.IsAssigned
            }
        };

        return dto;
    }

    /// <summary>
    /// Maps multiple vehicles to DTOs
    /// </summary>
    public List<VehicleDto> MapToDtos(List<VehicleResponse> vehicles)
    {
        return vehicles.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Maps a vehicle to a simplified summary
    /// </summary>
    public VehicleSummaryDto MapToSummary(VehicleResponse vehicle)
    {
        return new VehicleSummaryDto
        {
            Plate = vehicle.Plate,
            DisplayName = ProcessDisplayName(vehicle.CustomPlateNumber, vehicle.Plate),
            Status = vehicle.RawStatus?.StatusName ?? "Unknown",
            Speed = (int)((vehicle.RawStatus?.Speed ?? 0) / SPEED_DIVISOR),
            Company = vehicle.CompanyName,
            IsActive = vehicle.IsActive,
            LastUpdated = vehicle.UpdatedAt.ToString("dd-MM-yyyy HH:mm:ss")
        };
    }

    /// <summary>
    /// Maps multiple vehicles to summaries
    /// </summary>
    public List<VehicleSummaryDto> MapToSummaries(List<VehicleResponse> vehicles)
    {
        return vehicles.Select(MapToSummary).ToList();
    }

    /// <summary>
    /// Maps vehicles to a basic list format with essential information
    /// </summary>
    public List<object> MapToBasicList(List<VehicleResponse> vehicles)
    {
        return vehicles.Select(v => new
        {
            plate = v.Plate,
            customPlateNumber = v.CustomPlateNumber,
            vin = v.Vin,
            enginNo = v.EnginNo,
            vehicleTypeName = v.VehicleTypeName,
            vehicleGroupName = v.VehicleGroupName,
            simPhone = v.SimPhone,
            maxSpeed = v.MaxSpeed / SPEED_DIVISOR,
            description = v.Description,
            activeDate = v.ActiveDate?.ToString("dd-MM-yyyy HH:mm:ss"),
            updatedAt = v.UpdatedAt.ToString("dd-MM-yyyy HH:mm:ss")
        }).ToList<object>();
    }

    /// <summary>
    /// Generates fleet statistics from a list of vehicles
    /// </summary>
    public FleetStatisticsDto GenerateStatistics(List<VehicleResponse> vehicles)
    {
        var stats = new FleetStatisticsDto
        {
            TotalVehicles = vehicles.Count,
            ActiveVehicles = vehicles.Count(v => v.IsActive),
            InactiveVehicles = vehicles.Count(v => !v.IsActive),
            LastUpdated = DateTime.UtcNow
        };

        // Group by status
        stats.VehiclesByStatus = vehicles
            .Where(v => v.RawStatus != null)
            .GroupBy(v => v.RawStatus!.StatusName)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by company
        stats.VehiclesByCompany = vehicles
            .GroupBy(v => v.CompanyName)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by type
        stats.VehiclesByType = vehicles
            .GroupBy(v => v.VehicleTypeName)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate special conditions
        // Convert speeds for comparison
        stats.VehiclesOverSpeed = vehicles.Count(v =>
            v.RawStatus != null && (v.RawStatus.Speed / SPEED_DIVISOR) > (v.MaxSpeed / SPEED_DIVISOR));

        stats.VehiclesWithExpiredInsurance = vehicles.Count(v =>
            v.ExpiredInsuranceDate.HasValue && v.ExpiredInsuranceDate.Value < DateTime.UtcNow);

        stats.VehiclesWithExpiredRegistry = vehicles.Count(v =>
            v.ExpiredRegistryDate.HasValue && v.ExpiredRegistryDate.Value < DateTime.UtcNow);

        return stats;
    }

    /// <summary>
    /// Creates a search result DTO
    /// </summary>
    public VehicleSearchResultDto CreateSearchResult(
        List<VehicleResponse> vehicles,
        string searchCriteria)
    {
        return new VehicleSearchResultDto
        {
            TotalCount = vehicles.Count,
            Vehicles = MapToSummaries(vehicles),
            SearchCriteria = searchCriteria
        };
    }

    #region Private Helper Methods

    private string ProcessDisplayName(string customPlate, string regularPlate)
    {
        if (string.IsNullOrWhiteSpace(customPlate))
            return regularPlate;

        return customPlate;
    }

    private VehicleStatusDto MapStatus(VehicleResponse vehicle)
    {
        var status = new VehicleStatusDto
        {
            // Convert maxSpeed from raw value to km/h
            MaxSpeed = (int)(vehicle.MaxSpeed / SPEED_DIVISOR)
        };

        if (vehicle.RawStatus != null)
        {
            status.StatusName = vehicle.RawStatus.StatusName;
            status.StatusColor = vehicle.RawStatus.StatusColor;
            status.StatusCode = vehicle.RawStatus.Status;
            // Convert speed from raw value to km/h
            status.CurrentSpeed = (int)(vehicle.RawStatus.Speed / SPEED_DIVISOR);
            status.IsOverSpeed = status.CurrentSpeed > status.MaxSpeed;

            // Generate human-readable speed description
            status.SpeedDescription = status.CurrentSpeed switch
            {
                0 => "Stopped",
                > 0 when status.IsOverSpeed => $"Over speed! ({status.CurrentSpeed} km/h exceeds limit of {status.MaxSpeed} km/h)",
                > 0 => $"Moving ({status.CurrentSpeed} km/h)",
                _ => "Unknown"
            };
        }
        else
        {
            status.StatusName = "Unknown";
            status.SpeedDescription = "No status data available";
        }

        return status;
    }

    private LocationDto MapLocation(RawStatus? rawStatus)
    {
        var location = new LocationDto();

        if (rawStatus == null)
        {
            location.HasValidGps = false;
            location.FormattedCoordinates = "No GPS data";
            location.Address = "Unknown";
            return location;
        }

        // Convert from raw coordinates to degrees
        location.Latitude = rawStatus.Y / GPS_COORDINATE_DIVISOR;
        location.Longitude = rawStatus.X / GPS_COORDINATE_DIVISOR;
        location.GpsColor = rawStatus.GpsColor;
        location.HasValidGps = rawStatus.X != 0 && rawStatus.Y != 0;

        // Use address from Info field if available, otherwise fall back to coordinates
        location.Address = !string.IsNullOrEmpty(rawStatus.Info)
            ? rawStatus.Info
            : "Unknown location";

        // Format coordinates for display
        location.FormattedCoordinates = location.HasValidGps
            ? $"{location.Latitude:F6}, {location.Longitude:F6}"
            : "Invalid GPS";

        // Convert GPS timestamp: seconds since 2010-01-01 00:00:00
        if (rawStatus.GpsTime > 0)
        {
            try
            {
                var baseDate = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Local);
                location.GpsTimestamp = baseDate.AddSeconds(rawStatus.GpsTime);
            }
            catch
            {
                location.GpsTimestamp = null;
            }
        }

        return location;
    }

    private DateInfoDto MapDates(VehicleResponse vehicle)
    {
        var dates = new DateInfoDto
        {
            ActiveDate = vehicle.ActiveDate,
            InsuranceDate = vehicle.InsuranceDate,
            InsuranceExpiration = vehicle.ExpiredInsuranceDate,
            RegistryDate = vehicle.RegistryDate,
            RegistryExpiration = vehicle.ExpiredRegistryDate
        };

        var now = DateTime.UtcNow;

        // Calculate insurance expiration status
        if (vehicle.ExpiredInsuranceDate.HasValue)
        {
            dates.IsInsuranceExpired = vehicle.ExpiredInsuranceDate.Value < now;
            if (!dates.IsInsuranceExpired)
            {
                dates.DaysUntilInsuranceExpires = (int)(vehicle.ExpiredInsuranceDate.Value - now).TotalDays;
            }
        }

        // Calculate registry expiration status
        if (vehicle.ExpiredRegistryDate.HasValue)
        {
            dates.IsRegistryExpired = vehicle.ExpiredRegistryDate.Value < now;
            if (!dates.IsRegistryExpired)
            {
                dates.DaysUntilRegistryExpires = (int)(vehicle.ExpiredRegistryDate.Value - now).TotalDays;
            }
        }

        return dates;
    }

    #endregion
}
