using System.ComponentModel;
using System.Linq;
using McpVersionVer2.Models;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using ModelContextProtocol.Server;

namespace McpVersionVer2.Tools;

[McpServerToolType]
public class VehicleInfoTools
{
    private readonly VehicleService _vehicleService;
    private readonly VehicleMapperService _mapper;

    public VehicleInfoTools(VehicleService vehicleService, VehicleMapperService mapper)
    {
        _vehicleService = vehicleService;
        _mapper = mapper;
    }

    [McpServerTool, Description("VEHICLE REGISTRY: Get vehicle information from the fleet registry. Supports: all vehicles, by plate, by ID, by group/company name. Returns plate, type, group, max speed, and other vehicle details. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleInfo(
        [Description("Bearer token")] string bearerToken,
        [Description("Filter by license plate number. Optional.")] string? plate = null,
        [Description("Filter by vehicle ID. Optional.")] string? id = null,
        [Description("Filter by group/company name. Optional.")] string? group = null)
    {
        try
        {
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery($"{plate}{id}{group}");
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            if (!OutputSanitizer.IsValidBearerToken(bearerToken))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid bearer token format.", "INVALID_TOKEN");
            }

            var tokenHash = RateLimiter.GetTokenHash(bearerToken);
            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                return OutputSanitizer.CreateErrorResponse(rateLimitReason!, "RATE_LIMIT_EXCEEDED");
            }

            List<VehicleResponse> vehicles;

            if (!string.IsNullOrEmpty(plate))
            {
                var vehicle = await _vehicleService.GetVehicleByPlateAsync(bearerToken, plate)
                    .SafeGetSingleAsync("vehicle", $"plate '{plate}'");
                vehicles = new List<VehicleResponse> { vehicle };
            }
            else if (!string.IsNullOrEmpty(id))
            {
                var vehicle = await _vehicleService.GetVehicleByIdAsync(bearerToken, id)
                    .SafeGetSingleAsync("vehicle", $"ID '{id}'");
                vehicles = new List<VehicleResponse> { vehicle };
            }
            else if (!string.IsNullOrEmpty(group))
            {
                vehicles = await _vehicleService.GetVehiclesByCompanyAsync(bearerToken, group);
            }
            else
            {
                vehicles = await _vehicleService.GetVehiclesAsync(bearerToken);
            }

            if (vehicles == null || !vehicles.Any())
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = "No vehicles found in the fleet." });
            }

            object result;
            if (string.IsNullOrEmpty(plate) && string.IsNullOrEmpty(id) && string.IsNullOrEmpty(group))
            {
                result = _mapper.MapToBasicList(vehicles);
            }
            else
            {
                result = _mapper.MapToDtos(vehicles);
            }

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE REGISTRY ONLY: Get fleet statistics (total vehicles, by type, by group). Returns counts and breakdowns by vehicle type and group. REJECT: non-vehicle queries.")]
    public async Task<string> GetFleetStatistics(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery("");
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            if (!OutputSanitizer.IsValidBearerToken(bearerToken))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid bearer token format.", "INVALID_TOKEN");
            }

            var tokenHash = RateLimiter.GetTokenHash(bearerToken);
            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                return OutputSanitizer.CreateErrorResponse(rateLimitReason!, "RATE_LIMIT_EXCEEDED");
            }

            var vehicles = await _vehicleService.GetVehiclesAsync(bearerToken)
                .SafeGetListAsync("vehicles");
            if (!vehicles.Any())
            {
                return System.Text.Json.JsonSerializer.Serialize(new { message = "No vehicles found in the fleet. Unable to generate statistics." });
            }

            var stats = _mapper.GenerateStatistics(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(stats, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("VEHICLE REGISTRY ONLY: Get vehicles with expired or expiring soon insurance/registry. Returns vehicles with compliance status and days until expiry. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehiclesWithExpiredCompliance(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery("");
            if (!isValid)
            {
                return OutputSanitizer.CreateErrorResponse("This tool is ONLY for vehicle tracking queries. " + errorMessage, "OFF_TOPIC");
            }

            if (!OutputSanitizer.IsValidBearerToken(bearerToken))
            {
                return OutputSanitizer.CreateErrorResponse("Invalid bearer token format.", "INVALID_TOKEN");
            }

            var tokenHash = RateLimiter.GetTokenHash(bearerToken);
            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                return OutputSanitizer.CreateErrorResponse(rateLimitReason!, "RATE_LIMIT_EXCEEDED");
            }

            var vehicles = await _vehicleService.GetVehiclesWithExpiredComplianceAsync(bearerToken);
            var dtos = _mapper.MapToDtos(vehicles);
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                totalCount = dtos.Count,
                vehicles = dtos.Select(d => new
                {
                    plate = d.Plate,
                    displayName = d.DisplayName,
                    company = d.Company.Name,
                    insuranceExpired = d.Dates.IsInsuranceExpired,
                    registryExpired = d.Dates.IsRegistryExpired,
                    insuranceExpiresIn = d.Dates.DaysUntilInsuranceExpires,
                    registryExpiresIn = d.Dates.DaysUntilRegistryExpires
                })
            }, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
