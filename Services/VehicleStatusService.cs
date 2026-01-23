using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using McpVersionVer2.Models;

namespace McpVersionVer2.Services;

public class VehicleStatusService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public VehicleStatusService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration.GetValue<string>("ApiSettings:VehicleStatusUrl")
            ?? throw new InvalidOperationException("VehicleStatusUrl not configured in appsettings.json");
    }

    /// <summary>
    /// Get all vehicle statuses from the API
    /// </summary>
    public async Task<List<VehicleStatus>> GetVehicleStatusesAsync(string bearerToken, long time = 0)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/vehicle-status");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new VehicleStatusRequest { Time = time }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<VehicleStatusResponse>(content);

            return result?.Data?.Data ?? new List<VehicleStatus>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch vehicle statuses: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get vehicle status by plate number
    /// </summary>
    public async Task<VehicleStatus?> GetVehicleStatusByPlateAsync(string bearerToken, string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
        {
            throw new ArgumentException("Plate number cannot be empty or whitespace.", nameof(plate));
        }

        var vehicles = await GetVehicleStatusesAsync(bearerToken);
        return vehicles.FirstOrDefault(v => 
            v.Plate.Equals(plate, StringComparison.OrdinalIgnoreCase) ||
            v.CustomPlateNumber.Contains(plate, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get vehicle status by ID
    /// </summary>
    public async Task<VehicleStatus?> GetVehicleStatusByIdAsync(string bearerToken, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Vehicle ID cannot be empty or whitespace.", nameof(id));
        }

        var vehicles = await GetVehicleStatusesAsync(bearerToken);
        return vehicles.FirstOrDefault(v => v.Id == id);
    }

    /// <summary>
    /// Get vehicles by group name
    /// </summary>
    public async Task<List<VehicleStatus>> GetVehiclesByGroupAsync(string bearerToken, string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new ArgumentException("Group name cannot be empty or whitespace.", nameof(groupName));
        }

        var vehicles = await GetVehicleStatusesAsync(bearerToken);
        return vehicles.Where(v => 
            v.VehicleGroup?.Name.Contains(groupName, StringComparison.OrdinalIgnoreCase) ?? false)
            .ToList();
    }

    /// <summary>
    /// Get moving vehicles (status = 2)
    /// </summary>
    [Obsolete("Use GetVehicleStatusesAsync with FilterByStatus for better performance")]
    public async Task<List<VehicleStatus>> GetMovingVehiclesAsync(string bearerToken)
    {
        var vehicles = await GetVehicleStatusesAsync(bearerToken);
        return vehicles.Where(v => v.Status == 2).ToList();
    }

    /// <summary>
    /// Get stopped vehicles (status = 0)
    /// </summary>
    [Obsolete("Use GetVehicleStatusesAsync with FilterByStatus for better performance")]
    public async Task<List<VehicleStatus>> GetStoppedVehiclesAsync(string bearerToken)
    {
        var vehicles = await GetVehicleStatusesAsync(bearerToken);
        return vehicles.Where(v => v.Status == 0).ToList();
    }

    /// <summary>
    /// Get idle vehicles (status = 1)
    /// </summary>
    [Obsolete("Use GetVehicleStatusesAsync with FilterByStatus for better performance")]
    public async Task<List<VehicleStatus>> GetIdleVehiclesAsync(string bearerToken)
    {
        var vehicles = await GetVehicleStatusesAsync(bearerToken);
        return vehicles.Where(v => v.Status == 1).ToList();
    }

    /// <summary>
    /// Get vehicles exceeding their max speed
    /// </summary>
    [Obsolete("Use GetVehicleStatusesAsync with FilterByStatus for better performance")]
    public async Task<List<VehicleStatus>> GetOverSpeedingVehiclesAsync(string bearerToken)
    {
        var vehicles = await GetVehicleStatusesAsync(bearerToken);
        return vehicles.Where(v => v.MaxSpeed > 0 && v.Speed > v.MaxSpeed).ToList();
    }

    /// <summary>
    /// Filter vehicles by status without making additional API calls
    /// Use this method after fetching vehicles with GetVehicleStatusesAsync
    /// </summary>
    public List<VehicleStatus> FilterByStatus(List<VehicleStatus> vehicles, string? statusFilter)
    {
        if (string.IsNullOrEmpty(statusFilter))
            return vehicles;

        return statusFilter.ToLowerInvariant() switch
        {
            "moving" => vehicles.Where(v => v.Status == 2).ToList(),
            "stopped" => vehicles.Where(v => v.Status == 0).ToList(),
            "idle" => vehicles.Where(v => v.Status == 1).ToList(),
            "overspeeding" => vehicles.Where(v => v.MaxSpeed > 0 && v.Speed > v.MaxSpeed).ToList(),
            _ => vehicles
        };
    }

    /// <summary>
    /// Get vehicles by type
    /// </summary>
    public async Task<List<VehicleStatus>> GetVehiclesByTypeAsync(string bearerToken, string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException("Type name cannot be empty or whitespace.", nameof(typeName));
        }

        var vehicles = await GetVehicleStatusesAsync(bearerToken);
        return vehicles.Where(v => 
            v.VehicleTypeName.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
