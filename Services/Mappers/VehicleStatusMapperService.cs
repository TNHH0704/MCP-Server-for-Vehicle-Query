using McpVersionVer2.Models;
using McpVersionVer2.Models.Dto;
using McpVersionVer2.Utils;

namespace McpVersionVer2.Services.Mappers;

public class VehicleStatusMapperService : IMapper<VehicleStatus, RealTimeVehicleStatusSummaryDto>
{
    private readonly SecurityValidationService _securityService;
    private const double SPEED_DIVISOR = 100.0;
    private const double DISTANCE_DIVISOR = 1000.0;
    private const double COORDINATE_DIVISOR = 10000.0;

    public VehicleStatusMapperService(SecurityValidationService securityService)
    {
        _securityService = securityService;
    }

    /// <summary>
    /// Convert timestamp from custom epoch (1/1/2010 00:00:00) to DateTime
    /// </summary>
    private DateTime UnixToDateTime(long unixTime)
    {
        if (unixTime == 0) return DateTime.MinValue;
        return DateUtils.FromGpsEpoch((int)unixTime);
    }

    /// <summary>
    /// Get status name from status code
    /// </summary>
    private string GetStatusName(int statusCode)
    {
        return statusCode switch
        {
            0 => "Stop",
            1 => "Idle",
            2 => "Moving",
            _ => "Unknown"
        };
    }

    private (double lat, double lon) ConvertCoordinates(long x, long y)
    {
        return GisUtils.ConvertVehicleStatusCoordinates(x, y);
    }

    /// <summary>
    /// Map VehicleStatus to detailed DTO
    /// </summary>
    public RealTimeVehicleStatusDto MapToDto(VehicleStatus vehicle)
    {
        var (lat, lon) = ConvertCoordinates(vehicle.X, vehicle.Y);
        
        string statusName;
        if (vehicle.Speed > 0 && vehicle.Status == 0)
        {
            statusName = "Lost Signal";
        }
        else
        {
            statusName = vehicle.Speed > 0 ? "Running" : "Stop";
        }

        return new RealTimeVehicleStatusDto
        {
            Id = vehicle.Id,
            Plate = vehicle.Plate,
            DisplayName = vehicle.CustomPlateNumber,
            Group = new RealTimeVehicleGroupDto
            {
                Id = vehicle.VehicleGroup?.Id ?? string.Empty,
                Name = vehicle.VehicleGroup?.Name ?? "N/A"
            },
            VehicleType = vehicle.VehicleTypeName,
            Location = new RealTimeLocationDto
            {
                Latitude = lat,
                Longitude = lon,
                Altitude = vehicle.Z,
                Address = vehicle.Info,
                Heading = vehicle.Heading,
                SatelliteCount = vehicle.Satellite
            },
            Speed = new RealTimeSpeedDto
            {
                CurrentSpeed = vehicle.Speed,
                MaxSpeed = vehicle.MaxSpeed
            },
            Status = new RealTimeStatusDto
            {
                StatusCode = vehicle.Status,
                StatusName = statusName,
                StopOrIdleTime = vehicle.StopOrIdleTime,
                Input = vehicle.Input,
                IsRunning = vehicle.Speed > 0 && vehicle.Status == 1 
            },
            Device = new RealTimeDeviceDto
            {
                Imei = vehicle.Imei,
                DeviceTypeId = vehicle.DeviceTypeId,
                Voltage = vehicle.Voltage,
                Battery = vehicle.Battery
            },
            CurrentTrip = vehicle.Trip != null ? new RealTimeTripDto
            {
                FromTime = UnixToDateTime(vehicle.Trip.FromTime),
                ToTime = UnixToDateTime(vehicle.Trip.ToTime),
                FromAddress = vehicle.Trip.From,
                ToAddress = vehicle.Trip.To,
                Distance = vehicle.Trip.GpsMileage,
                Duration = vehicle.Trip.Duration,
                MaxSpeed = vehicle.Trip.MaxSpeed,
                AverageSpeed = vehicle.Trip.AverageSpeed
            } : null,
            DailyStats = new RealTimeDailyStatsDto
            {
                EngineOffCount = vehicle.Daily?.StopCount ?? 0,  
                VehicleStopCount = vehicle.Daily?.IdleCount ?? 0,  
                TotalDistance = vehicle.Daily?.GpsMileage ?? 0,
                RunTime = vehicle.Daily?.RunTime ?? 0,
                IdleTime = vehicle.Daily?.IdleTime ?? 0,
                StopTime = vehicle.Daily?.StopTime ?? 0,
                MaxSpeed = vehicle.Daily?.MaxSpeed ?? 0,
                OverSpeedCount = vehicle.Daily?.OverSpeed ?? 0
            },
            Timestamps = new RealTimeTimestampsDto
            {
                GpsTime = UnixToDateTime(vehicle.GpsTime),  
                LastUpdateTime = UnixToDateTime(vehicle.LastUpdateTime),  
                AccOffTime = vehicle.AccOffTime > 0 ? UnixToDateTime(vehicle.AccOffTime) : null
            }
        };
    }

    /// <summary>
    /// Map list of VehicleStatus to DTOs
    /// </summary>
    public List<RealTimeVehicleStatusDto> MapToDtos(List<VehicleStatus> vehicles)
    {
        return vehicles.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Map VehicleStatus to summary DTO
    /// </summary>
    public RealTimeVehicleStatusSummaryDto MapToSummary(VehicleStatus vehicle)
    {
        string status;
        if (vehicle.Speed > 0 && vehicle.Status == 0)
        {
            status = "Lost Signal";
        }
        else
        {
            status = vehicle.Speed > 0 ? "Running" : "Stop";
        }
        
        return new RealTimeVehicleStatusSummaryDto
        {
            Plate = _securityService.SanitizeOutput(vehicle.Plate),
            DisplayName = _securityService.SanitizeOutput(vehicle.CustomPlateNumber),
            Group = _securityService.SanitizeOutput(vehicle.VehicleGroup?.Name ?? "N/A"),
            Status = status,
            Speed = $"{vehicle.Speed / SPEED_DIVISOR:F1} km/h",
            MaxSpeed = $"{vehicle.MaxSpeed / SPEED_DIVISOR:F1} km/h",
            Location = _securityService.SanitizeOutput(string.IsNullOrEmpty(vehicle.Info) ? "Unknown" : vehicle.Info),
            LastUpdate = DateUtils.FormatForApi(UnixToDateTime(vehicle.GpsTime)),
            LastStopTime = DateUtils.FormatForApi(UnixToDateTime(vehicle.LastUpdateTime)),
            IsRunning = vehicle.Speed > 0 && vehicle.Status == 1
        };
    }

    /// <summary>
    /// Map list to summary DTOs
    /// </summary>
    public List<RealTimeVehicleStatusSummaryDto> MapToSummaries(List<VehicleStatus> vehicles)
    {
        return this.MapList(vehicles);
    }

    /// <summary>
    /// Map single vehicle to summary (IMapper implementation)
    /// </summary>
    RealTimeVehicleStatusSummaryDto IMapper<VehicleStatus, RealTimeVehicleStatusSummaryDto>.MapToDto(VehicleStatus source)
    {
        return MapToSummary(source);
    }

    /// <summary>
    /// Map multiple vehicles to summaries (IMapper implementation)
    /// </summary>
    List<RealTimeVehicleStatusSummaryDto> IMapper<VehicleStatus, RealTimeVehicleStatusSummaryDto>.MapToDtos(IEnumerable<VehicleStatus> sources)
    {
        return sources.Select(MapToSummary).ToList();
    }

    /// <summary>
    /// Map VehicleStatus to daily statistics summary DTO
    /// </summary>
    public DailyStatisticsSummaryDto MapToDailyStatsSummary(VehicleStatus vehicle)
    {
        return new DailyStatisticsSummaryDto
        {
            Plate = _securityService.SanitizeOutput(vehicle.Plate),
            DisplayName = _securityService.SanitizeOutput(vehicle.CustomPlateNumber),
            GpsMileage = vehicle.Daily != null ? $"{vehicle.Daily.GpsMileage / DISTANCE_DIVISOR:F2} km" : "0.00 km",
            RunTime = vehicle.Daily != null ? TimeSpan.FromSeconds(vehicle.Daily.RunTime).ToString(@"hh\:mm\:ss") : "00:00:00",
            EngineOffCount = vehicle.Daily?.StopCount ?? 0,
            VehicleStopCount = vehicle.Daily?.IdleCount ?? 0,
            OverSpeed = vehicle.Daily?.OverSpeed ?? 0,
            MaxSpeed = vehicle.Daily != null ? $"{vehicle.Daily.MaxSpeed / SPEED_DIVISOR:F1} km/h" : "0.0 km/h",
            IdleTime = vehicle.Daily != null ? TimeSpan.FromSeconds(vehicle.Daily.IdleTime).ToString(@"hh\:mm\:ss") : "00:00:00",
            StopTime = vehicle.Daily != null ? TimeSpan.FromSeconds(vehicle.Daily.StopTime).ToString(@"hh\:mm\:ss") : "00:00:00"
        };
    }

    /// <summary>
    /// Map list to daily statistics summary DTOs
    /// </summary>
    public List<DailyStatisticsSummaryDto> MapToDailyStatsSummaries(List<VehicleStatus> vehicles)
    {
        return vehicles.Select(MapToDailyStatsSummary).ToList();
    }

    /// <summary>
    /// Create search result
    /// </summary>
    public RealTimeVehicleStatusSearchResult CreateSearchResult(List<VehicleStatus> vehicles, string criteria)
    {
        return (RealTimeVehicleStatusSearchResult)MapperHelpers.CreateSearchResult(MapToSummaries(vehicles), criteria);
    }
}