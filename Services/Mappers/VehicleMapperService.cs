using McpVersionVer2.Models;
using McpVersionVer2.Models.Dto;
using McpVersionVer2.Utils;

namespace McpVersionVer2.Services.Mappers;

/// <summary>
/// Service for mapping raw vehicle data to processed DTOs
/// </summary>
public class VehicleMapperService : IMapper<VehicleResponse, VehicleSummaryDto>
{
    private readonly SecurityValidationService _securityService;
    private const double SPEED_DIVISOR = 100.0;
    private const double DISTANCE_DIVISOR = 1000.0;
    private const double GPS_COORDINATE_DIVISOR = 1_000_000.0;

    public VehicleMapperService(SecurityValidationService securityService)
    {
        _securityService = securityService;
    }

    /// <summary>
    /// Maps a raw VehicleResponse to a processed VehicleDto
    /// </summary>
    public VehicleDto MapToDto(VehicleResponse vehicle)
    {
        var dto = new VehicleDto
        {
            Id = vehicle.Id,
            Plate = _securityService.SanitizeOutput(vehicle.Plate),
            DisplayName = _securityService.SanitizeOutput(ProcessDisplayName(vehicle.CustomPlateNumber, vehicle.Plate)),
            VehicleType = _securityService.SanitizeOutput(vehicle.VehicleTypeName),
            VehicleGroup = _securityService.SanitizeOutput(vehicle.VehicleGroupName),
            Description = _securityService.SanitizeOutput(vehicle.Description),
            LastUpdatedBy = _securityService.SanitizeOutput(vehicle.UpdatedByName),

            Company = new CompanyInfo
            {
                Id = vehicle.CompanyId,
                Name = _securityService.SanitizeOutput(vehicle.CompanyName),
                Phone = _securityService.SanitizeOutput(vehicle.CompanyPhone)
            },

            Device = new DeviceInfo
            {
                Id = vehicle.DeviceId,
                Type = _securityService.SanitizeOutput(vehicle.DeviceTypeName),
                Imei = _securityService.SanitizeOutput(vehicle.Imei),
                SimPhone = _securityService.SanitizeOutput(vehicle.SimPhone),
                Iccid = _securityService.SanitizeOutput(vehicle.Iccid),
                FirmwareVersion = _securityService.SanitizeOutput(vehicle.FirmwareVersion)
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
            Plate = _securityService.SanitizeOutput(vehicle.Plate),
            DisplayName = _securityService.SanitizeOutput(ProcessDisplayName(vehicle.CustomPlateNumber, vehicle.Plate)),
            Status = _securityService.SanitizeOutput(vehicle.RawStatus?.StatusName ?? "Unknown"),
            Speed = (int)((vehicle.RawStatus?.Speed ?? 0) / SPEED_DIVISOR),
            Company = _securityService.SanitizeOutput(vehicle.CompanyName),
            IsActive = vehicle.IsActive,
            LastUpdated = DateUtils.FormatForApi(vehicle.UpdatedAt)
        };
    }

    /// <summary>
    /// Maps multiple vehicles to summaries
    /// </summary>
    public List<VehicleSummaryDto> MapToSummaries(List<VehicleResponse> vehicles)
    {
        return this.MapList(vehicles);
    }

    /// <summary>
    /// Maps a single vehicle to summary (IMapper implementation)
    /// </summary>
    VehicleSummaryDto IMapper<VehicleResponse, VehicleSummaryDto>.MapToDto(VehicleResponse source)
    {
        return MapToSummary(source);
    }

    /// <summary>
    /// Maps multiple vehicles to summaries (IMapper implementation)
    /// </summary>
    List<VehicleSummaryDto> IMapper<VehicleResponse, VehicleSummaryDto>.MapToDtos(IEnumerable<VehicleResponse> sources)
    {
        return sources.Select(MapToSummary).ToList();
    }

    /// <summary>
    /// Maps vehicles to a basic list format with essential information
    /// </summary>
    public List<object> MapToBasicList(List<VehicleResponse> vehicles)
    {
        return vehicles.Select(v => new
        {
            plate = _securityService.SanitizeOutput(v.Plate),
            customPlateNumber = _securityService.SanitizeOutput(v.CustomPlateNumber),
            vin = _securityService.SanitizeOutput(v.Vin),
            enginNo = _securityService.SanitizeOutput(v.EnginNo),
            vehicleTypeName = _securityService.SanitizeOutput(v.VehicleTypeName),
            vehicleGroupName = _securityService.SanitizeOutput(v.VehicleGroupName),
            simPhone = _securityService.SanitizeOutput(v.SimPhone),
            maxSpeed = v.MaxSpeed / SPEED_DIVISOR,
            description = _securityService.SanitizeOutput(v.Description),
            activeDate = v.ActiveDate.HasValue ? DateUtils.FormatForApi(v.ActiveDate.Value) : null,
            updatedAt = DateUtils.FormatForApi(v.UpdatedAt)
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

        stats.VehiclesByStatus = vehicles
            .Where(v => v.RawStatus != null)
            .GroupBy(v => v.RawStatus!.StatusName)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.VehiclesByCompany = vehicles
            .GroupBy(v => v.CompanyName)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.VehiclesByType = vehicles
            .GroupBy(v => v.VehicleTypeName)
            .ToDictionary(g => g.Key, g => g.Count());

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
        return (VehicleSearchResultDto)MapperHelpers.CreateSearchResult(MapToSummaries(vehicles), searchCriteria);
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
            MaxSpeed = (int)(vehicle.MaxSpeed / SPEED_DIVISOR)
        };

        if (vehicle.RawStatus != null)
        {
            status.StatusName = vehicle.RawStatus.StatusName;
            status.StatusColor = vehicle.RawStatus.StatusColor;
            status.StatusCode = vehicle.RawStatus.Status;
            status.CurrentSpeed = (int)(vehicle.RawStatus.Speed / SPEED_DIVISOR);
            status.IsOverSpeed = status.CurrentSpeed > status.MaxSpeed;

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

        var (latitude, longitude) = GisUtils.ConvertGpsCoordinates(rawStatus.X, rawStatus.Y);
        location.Latitude = latitude;
        location.Longitude = longitude;
        location.GpsColor = rawStatus.GpsColor;
        location.HasValidGps = GisUtils.IsValidCoordinate(latitude, longitude);

        location.Address = !string.IsNullOrEmpty(rawStatus.Info)
            ? _securityService.SanitizeOutput(rawStatus.Info)
            : "Unknown location";

        location.FormattedCoordinates = location.HasValidGps
            ? $"{location.Latitude:F6}, {location.Longitude:F6}"
            : "Invalid GPS";

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

        if (vehicle.ExpiredInsuranceDate.HasValue)
        {
            dates.IsInsuranceExpired = vehicle.ExpiredInsuranceDate.Value < now;
            if (!dates.IsInsuranceExpired)
            {
                dates.DaysUntilInsuranceExpires = (int)(vehicle.ExpiredInsuranceDate.Value - now).TotalDays;
            }
        }

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