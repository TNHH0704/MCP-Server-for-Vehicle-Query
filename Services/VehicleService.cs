using System.Net.Http.Headers;
using System.Text.Json;
using McpVersionVer2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace McpVersionVer2.Services;

public class VehicleService
{
    private readonly string _vehiclesByUserUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger<VehicleService> _logger;
    private List<VehicleResponse>? _cachedVehicles;
    private DateTime _lastCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public VehicleService(HttpClient httpClient, ILogger<VehicleService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _vehiclesByUserUrl = configuration.GetValue<string>("ApiSettings:VehicleApiUrl")
            ?? throw new InvalidOperationException("VehicleApiUrl not configured in appsettings.json");
    }

    public async Task<List<VehicleResponse>> GetVehiclesAsync(string bearerToken, bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedVehicles != null && DateTime.UtcNow - _lastCacheTime < _cacheExpiration)
        {
            return _cachedVehicles;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, _vehiclesByUserUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<VehicleResponse>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (apiResponse == null || !apiResponse.Success || apiResponse.Data == null)
        {
            throw new Exception("Invalid API response or unsuccessful request");
        }

        _cachedVehicles = apiResponse.Data;
        _lastCacheTime = DateTime.UtcNow;

        return _cachedVehicles;
    }

    public async Task<VehicleResponse?> GetVehicleByPlateAsync(string bearerToken, string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
        {
            throw new ArgumentException("Plate number cannot be empty or whitespace.", nameof(plate));
        }

        var vehicles = await GetVehiclesAsync(bearerToken);
        return vehicles.FirstOrDefault(v => 
            v.Plate.Equals(plate, StringComparison.OrdinalIgnoreCase) ||
            v.CustomPlateNumber.Contains(plate, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<VehicleResponse?> GetVehicleByIdAsync(string bearerToken, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Vehicle ID cannot be empty or whitespace.", nameof(id));
        }

        var vehicles = await GetVehiclesAsync(bearerToken);
        return vehicles.FirstOrDefault(v => v.Id == id);
    }

    public async Task<List<VehicleResponse>> GetVehiclesByStatusAsync(string bearerToken, string statusName)
    {
        var vehicles = await GetVehiclesAsync(bearerToken);
        return vehicles.Where(v => 
            v.RawStatus != null && 
            v.RawStatus.StatusName.Equals(statusName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<VehicleResponse>> GetVehiclesByCompanyAsync(string bearerToken, string companyName)
    {
        var vehicles = await GetVehiclesAsync(bearerToken);
        return vehicles.Where(v => 
            v.VehicleGroupName.Contains(companyName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<VehicleResponse>> GetOverSpeedVehiclesAsync(string bearerToken)
    {
        var vehicles = await GetVehiclesAsync(bearerToken);
        // Convert speeds (divide by 100) for comparison
        return vehicles.Where(v => 
            v.RawStatus != null && (v.RawStatus.Speed / 100) > (v.MaxSpeed / 100))
            .ToList();
    }

    public async Task<List<VehicleResponse>> GetVehiclesWithExpiredComplianceAsync(string bearerToken)
    {
        var vehicles = await GetVehiclesAsync(bearerToken);
        return vehicles.Where(v => 
            (v.ExpiredInsuranceDate.HasValue && v.ExpiredInsuranceDate.Value < DateTime.UtcNow) ||
            (v.ExpiredRegistryDate.HasValue && v.ExpiredRegistryDate.Value < DateTime.UtcNow))
            .ToList();
    }
}
