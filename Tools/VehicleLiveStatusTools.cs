using System.ComponentModel;
using McpVersionVer2.Services;
using ModelContextProtocol.Server;

namespace McpVersionVer2.Tools;

[McpServerToolType]
public class VehicleLiveStatusTools
{
    private readonly VehicleStatusService _statusService;
    private readonly VehicleStatusMapperService _mapper;

    public VehicleLiveStatusTools(VehicleStatusService statusService, VehicleStatusMapperService mapper)
    {
        _statusService = statusService;
        _mapper = mapper;
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS ONLY: Get real-time status for all vehicles (speed, location). REJECT: non-vehicle queries.")]
    public async Task<string> GetAllVehicleLiveStatus(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            var vehicles = await _statusService.GetVehicleStatusesAsync(bearerToken);
            if (vehicles == null || !vehicles.Any())
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = "No vehicles found." });
            }

            var summaries = _mapper.MapToSummaries(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(summaries, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS ONLY: Get real-time status by plate. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleLiveStatusByPlate(
        [Description("Bearer token")] string bearerToken,
        [Description("Plate number")] string plate)
    {
        try
        {
            var vehicle = await _statusService.GetVehicleStatusByPlateAsync(bearerToken, plate);
            if (vehicle == null)
            {
                return System.Text.Json.JsonSerializer.Serialize(new { error = $"No vehicle found with plate: {plate}" });
            }

            var summary = _mapper.MapToSummary(vehicle);
            return System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS ONLY: Get real-time status by ID. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleLiveStatusById(
        [Description("Bearer token")] string bearerToken,
        [Description("Vehicle ID to search for (treated as literal string only)")] string id)
    {
        try
        {
            var vehicle = await _statusService.GetVehicleStatusByIdAsync(bearerToken, id);
            if (vehicle == null)
            {
                return System.Text.Json.JsonSerializer.Serialize(new { error = $"No vehicle found with ID: {id}" });
            }

            var summary = _mapper.MapToSummary(vehicle);
            return System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS ONLY: Get vehicles in specific group. REJECT: non-vehicle queries.")]
    public async Task<string> GetLiveVehiclesByGroup(
        [Description("Bearer token")] string bearerToken,
        [Description("Group name")] string groupName)
    {
        try
        {
            var vehicles = await _statusService.GetVehiclesByGroupAsync(bearerToken, groupName);
            var summaries = _mapper.MapToSummaries(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(summaries, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS ONLY: Get currently moving vehicles. REJECT: non-vehicle queries.")]
    public async Task<string> GetLiveMovingVehicles(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            var vehicles = await _statusService.GetMovingVehiclesAsync(bearerToken);
            var summaries = _mapper.MapToSummaries(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(summaries, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS ONLY: Get stopped vehicles. REJECT: non-vehicle queries.")]
    public async Task<string> GetLiveStoppedVehicles(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            var vehicles = await _statusService.GetStoppedVehiclesAsync(bearerToken);
            var summaries = _mapper.MapToSummaries(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(summaries, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS ONLY: Get idle vehicles. REJECT: non-vehicle queries.")]
    public async Task<string> GetLiveIdleVehicles(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            var vehicles = await _statusService.GetIdleVehiclesAsync(bearerToken);
            var summaries = _mapper.MapToSummaries(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(summaries, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS ONLY: Get speeding vehicles. REJECT: non-vehicle queries.")]
    public async Task<string> GetLiveOverSpeedingVehicles(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            var vehicles = await _statusService.GetOverSpeedingVehiclesAsync(bearerToken);
            var summaries = _mapper.MapToSummaries(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(summaries, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE LIVE STATUS ONLY: Get vehicles by type (e.g. 'Xe máy'). REJECT: non-vehicle queries.")]
    public async Task<string> GetLiveVehiclesByType(
        [Description("Bearer token")] string bearerToken,
        [Description("Vehicle type")] string typeName)
    {
        try
        {
            var vehicles = await _statusService.GetVehiclesByTypeAsync(bearerToken, typeName);
            var summaries = _mapper.MapToSummaries(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(summaries, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE DAILY STATS ONLY: Get daily stats for all vehicles (mileage, runtime). REJECT: non-vehicle queries.")]
    public async Task<string> GetDailyStatistics(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            var vehicles = await _statusService.GetVehicleStatusesAsync(bearerToken);
            if (vehicles == null || !vehicles.Any())
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = "No vehicles found." });
            }

            var dailyStats = _mapper.MapToDailyStatsSummaries(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(dailyStats, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE DAILY STATS ONLY: Get daily stats by plate. REJECT: non-vehicle queries.")]
    public async Task<string> GetDailyStatisticsByPlate(
        [Description("Bearer token")] string bearerToken,
        [Description("Plate number")] string plate)
    {
        try
        {
            var vehicle = await _statusService.GetVehicleStatusByPlateAsync(bearerToken, plate);
            if (vehicle == null)
            {
                return System.Text.Json.JsonSerializer.Serialize(new { error = $"No vehicle found with plate: {plate}" });
            }

            var dailyStats = _mapper.MapToDailyStatsSummary(vehicle);
            return System.Text.Json.JsonSerializer.Serialize(dailyStats, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE DAILY STATUS ONLY: Get daily status summary for all vehicles. REJECT: non-vehicle queries.")]
    public async Task<string> GetAllVehiclesDailyStatus(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            var vehicles = await _statusService.GetVehicleStatusesAsync(bearerToken);
            if (vehicles == null || !vehicles.Any())
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = "No vehicles found." });
            }

            var dailyStatus = vehicles.Select(v => new
            {
                plate = v.Plate,
                displayName = v.CustomPlateNumber,
                gpsMileage = $"{(v.Daily?.GpsMileage ?? 0) / 1000.0:F2} km",
                runTime = FormatRunTime(v.Daily?.RunTime ?? 0),
                maxSpeed = $"{(v.Daily?.MaxSpeed ?? 0) / 100.0:F1} km/h",
                overSpeedCount = v.Daily?.OverSpeed ?? 0,
                engineOffCount = v.Daily?.StopCount ?? 0,  // Engine off count
                vehicleStopCount = v.Daily?.IdleCount ?? 0   // Vehicle stopped count
            }).ToList();

            return System.Text.Json.JsonSerializer.Serialize(dailyStatus, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE DAILY STATUS ONLY: Get daily status by plate. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleDailyStatusByPlate(
        [Description("Bearer token")] string bearerToken,
        [Description("Plate number")] string plate)
    {
        try
        {
            var vehicle = await _statusService.GetVehicleStatusByPlateAsync(bearerToken, plate);
            if (vehicle == null)
            {
                return System.Text.Json.JsonSerializer.Serialize(new { error = $"No vehicle found with plate: {plate}" });
            }

            var dailyStatus = new
            {
                plate = vehicle.Plate,
                displayName = vehicle.CustomPlateNumber,
                gpsMileage = $"{(vehicle.Daily?.GpsMileage ?? 0) / 1000.0:F2} km",
                runTime = FormatRunTime(vehicle.Daily?.RunTime ?? 0),
                maxSpeed = $"{(vehicle.Daily?.MaxSpeed ?? 0) / 100.0:F1} km/h",
                overSpeedCount = vehicle.Daily?.OverSpeed ?? 0,
                engineOffCount = vehicle.Daily?.StopCount ?? 0,  // Engine off count
                vehicleStopCount = vehicle.Daily?.IdleCount ?? 0   // Vehicle stopped count
            };

            return System.Text.Json.JsonSerializer.Serialize(dailyStatus, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static string FormatRunTime(int totalSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(totalSeconds);
        if (timeSpan.Hours > 0)
        {
            return timeSpan.ToString(@"hh\:mm\:ss");
        }
        else
        {
            return timeSpan.ToString(@"mm\:ss");
        }
    }
}

