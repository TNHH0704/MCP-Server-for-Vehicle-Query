using System.Text.Json.Serialization;

namespace McpVersionVer2.Models.Dto;

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;
}

public class VehicleResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("plate")]
    public string Plate { get; set; } = string.Empty;

    [JsonPropertyName("customPlateNumber")]
    public string CustomPlateNumber { get; set; } = string.Empty;

    [JsonPropertyName("vin")]
    public string Vin { get; set; } = string.Empty;

    [JsonPropertyName("enginNo")]
    public string EnginNo { get; set; } = string.Empty;

    [JsonPropertyName("productYear")]
    public int ProductYear { get; set; }

    [JsonPropertyName("vehicleTypeId")]
    public int VehicleTypeId { get; set; }

    [JsonPropertyName("vehicleTypeName")]
    public string VehicleTypeName { get; set; } = string.Empty;

    [JsonPropertyName("vehicleGroupId")]
    public string VehicleGroupId { get; set; } = string.Empty;

    [JsonPropertyName("vehicleGroupName")]
    public string VehicleGroupName { get; set; } = string.Empty;

    [JsonPropertyName("maxSpeed")]
    public int MaxSpeed { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("simId")]
    public string SimId { get; set; } = string.Empty;

    [JsonPropertyName("simPhone")]
    public string SimPhone { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("deviceTypeName")]
    public string DeviceTypeName { get; set; } = string.Empty;

    [JsonPropertyName("firmwareVersion")]
    public string FirmwareVersion { get; set; } = string.Empty;

    [JsonPropertyName("iccid")]
    public string Iccid { get; set; } = string.Empty;

    [JsonPropertyName("imei")]
    public string Imei { get; set; } = string.Empty;

    [JsonPropertyName("companyId")]
    public string CompanyId { get; set; } = string.Empty;

    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("companyPhone")]
    public string CompanyPhone { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public int Icon { get; set; }

    [JsonPropertyName("isLocked")]
    public bool IsLocked { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isAssigned")]
    public bool IsAssigned { get; set; }

    [JsonPropertyName("insuranceDate")]
    public DateTime? InsuranceDate { get; set; }

    [JsonPropertyName("expiredInsuranceDate")]
    public DateTime? ExpiredInsuranceDate { get; set; }

    [JsonPropertyName("activeDate")]
    public DateTime? ActiveDate { get; set; }

    [JsonPropertyName("registryDate")]
    public DateTime? RegistryDate { get; set; }

    [JsonPropertyName("expiredRegistryDate")]
    public DateTime? ExpiredRegistryDate { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("updatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;

    [JsonPropertyName("updatedByName")]
    public string UpdatedByName { get; set; } = string.Empty;

    [JsonPropertyName("rawStatus")]
    public RawStatus? RawStatus { get; set; }
}

public class RawStatus
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusName")]
    public string StatusName { get; set; } = string.Empty;

    [JsonPropertyName("statusColor")]
    public string StatusColor { get; set; } = string.Empty;

    [JsonPropertyName("gpsColor")]
    public string GpsColor { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public long X { get; set; }

    [JsonPropertyName("y")]
    public long Y { get; set; }

    [JsonPropertyName("gpsTime")]
    public long GpsTime { get; set; }

    [JsonPropertyName("speed")]
    public int Speed { get; set; }

    [JsonPropertyName("info")]
    public string Info { get; set; } = string.Empty;
}