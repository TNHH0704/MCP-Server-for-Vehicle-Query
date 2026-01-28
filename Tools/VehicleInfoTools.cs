using System.ComponentModel;
using McpVersionVer2.Services;
using McpVersionVer2.Services.Mappers;
using McpVersionVer2.Security;
using McpVersionVer2.Helpers;
using ModelContextProtocol.Server;
using static McpVersionVer2.Services.AppJsonSerializerOptions;

namespace McpVersionVer2.Tools;

[McpServerToolType]
public class VehicleInfoTools
{
    private readonly VehicleService _vehicleService;
    private readonly VehicleMapperService _mapper;
    private readonly SecurityValidationService _securityService;
    private readonly IConversationContextService _contextService;
    private readonly RequestContextService _requestContext;

    public VehicleInfoTools(
        VehicleService vehicleService, 
        VehicleMapperService mapper, 
        SecurityValidationService securityService,
        IConversationContextService contextService,
        RequestContextService requestContext)
    {
        _vehicleService = vehicleService;
        _mapper = mapper;
        _securityService = securityService;
        _contextService = contextService;
        _requestContext = requestContext;
        _contextService = contextService;
        _requestContext = requestContext;
    }

    [McpServerTool, Description("VEHICLE REGISTRY: Get vehicle information from fleet registry. Supports: all vehicles, by plate, by ID, by group/company name. Returns plate, type, group, max speed, and other vehicle details. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehicleInfo(
        [Description("Bearer token")] string bearerToken,
        [Description("Filter by license plate number. Optional.")] string? plate = null,
        [Description("Filter by vehicle ID. Optional.")] string? id = null,
        [Description("Filter by group/company name. Optional.")] string? group = null)
    {
        var queryContext = $"GetVehicleInfo plate:{plate ?? ""} id:{id ?? ""} group:{group ?? ""}";

        return await ToolExecutionHelper.ExecuteValidatedToolRequestWithContextAsync(
            securityService: _securityService,
            queryContext: queryContext,
            domain: "vehicle_registry",
            bearerToken: bearerToken,
            contextService: _contextService,
            requestContext: _requestContext,
            action: async (token) => 
            {
                var vehicles = await _vehicleService.GetVehiclesWithFilterAsync(token, plate, id, group);
                vehicles.RequireNonEmptyResult("vehicles");
                return vehicles;
            },
            successResponse: (vehicles) => 
            {
                object result = (plate == null && id == null && group == null)
                    ? _mapper.MapToBasicList(vehicles)
                    : _mapper.MapToDtos(vehicles);
                return System.Text.Json.JsonSerializer.Serialize(result, Default);
            });
    }

    [McpServerTool, Description("VEHICLE REGISTRY ONLY: Get fleet statistics (total vehicles, by type, by group). Returns counts and breakdowns by vehicle type and group. REJECT: non-vehicle queries.")]
    public async Task<string> GetFleetStatistics(
        [Description("Bearer token")] string bearerToken)
    {
        var queryContext = "GetFleetStatistics fleet statistics";

        return await ToolExecutionHelper.ExecuteValidatedToolRequestWithContextAsync(
            securityService: _securityService,
            queryContext: queryContext,
            domain: "vehicle_registry",
            bearerToken: bearerToken,
            contextService: _contextService,
            requestContext: _requestContext,
            action: async (token) => 
            {
                var vehicles = await _vehicleService.GetVehiclesWithFilterAsync(token);
                vehicles.RequireNonEmptyResult("vehicles");
                return _mapper.GenerateStatistics(vehicles);
            },
            successResponse: (stats) => System.Text.Json.JsonSerializer.Serialize(stats, Default));
    }

    [McpServerTool, Description("VEHICLE REGISTRY ONLY: Get vehicles with expired or expiring soon insurance/registry. Returns vehicles with compliance status and days until expiry. REJECT: non-vehicle queries.")]
    public async Task<string> GetVehiclesWithExpiredCompliance(
        [Description("Bearer token")] string bearerToken)
    {
        var queryContext = "GetVehiclesWithExpiredCompliance compliance insurance registry expired";

        return await ToolExecutionHelper.ExecuteValidatedToolRequestWithContextAsync(
            securityService: _securityService,
            queryContext: queryContext,
            domain: "vehicle_registry",
            bearerToken: bearerToken,
            contextService: _contextService,
            requestContext: _requestContext,
            action: async (token) => 
            {
                var vehicles = await _vehicleService.GetVehiclesWithFilterAsync(token);
                return _mapper.MapToDtos(vehicles);
            },
            successResponse: (dtos) => System.Text.Json.JsonSerializer.Serialize(new
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
            }, Default));
    }
}
