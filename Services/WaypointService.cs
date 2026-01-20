using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProtoBuf;

namespace McpVersionVer2.Services;

public class WaypointService
{
    private readonly string _waypointApiUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WaypointService> _logger;

    public WaypointService(HttpClient httpClient, ILogger<WaypointService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _waypointApiUrl = configuration.GetValue<string>("ApiSettings:WaypointApiUrl")
            ?? throw new InvalidOperationException("WaypointApiUrl not configured in appsettings.json");
    }

    /// <summary>
    /// Fetches and decompresses waypoint history for a vehicle within a time range
    /// </summary>
    public async Task<List<Waypoint>> GetVehicleWaypointsAsync(
        string bearerToken,
        string vehicleId,
        DateTime startTime,
        DateTime endTime)
    {
        var startTimeEncoded = Uri.EscapeDataString(startTime.ToString("yyyy-MM-ddTHH:mm:ss"));
        var endTimeEncoded = Uri.EscapeDataString(endTime.ToString("yyyy-MM-ddTHH:mm:ss"));
        var url = $"{_waypointApiUrl}/{vehicleId}/{startTimeEncoded}/{endTimeEncoded}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        var jsonDoc = JsonDocument.Parse(content);
        var compressedWaypoints = jsonDoc.RootElement
            .GetProperty("data")
            .GetProperty("compressedWaypoints")
            .GetString();

        if (string.IsNullOrEmpty(compressedWaypoints))
        {
            return new List<Waypoint>();
        }

        return DecompressWaypoints(compressedWaypoints);
    }

    /// <summary>
    /// Decompresses waypoint data following the multi-layer compression scheme:
    /// Base64 (outer) → GZip → Base64 (inner) → Protobuf
    /// </summary>
    private List<Waypoint> DecompressWaypoints(string outerBase64)
    {
        try
        {
            // Remove whitespace from Base64 string
            outerBase64 = new string(outerBase64.Where(c => !char.IsWhiteSpace(c)).ToArray());

            if (string.IsNullOrEmpty(outerBase64))
            {
                return new List<Waypoint>();
            }

            // Step 1: Decode outer Base64 layer
            byte[] compressedBytes = Convert.FromBase64String(outerBase64);

            if (compressedBytes.Length == 0)
            {
                return new List<Waypoint>();
            }

            // Check for custom 4-byte header before GZip magic bytes (1F 8B)
            int offset = HasGzipHeader(compressedBytes) ? 4 : 0;

            // Step 2: Decompress GZip layer to get inner Base64 string
            string innerBase64 = DecompressGzipLayer(compressedBytes, offset);

            if (string.IsNullOrEmpty(innerBase64))
            {
                return new List<Waypoint>();
            }

            // Step 3: Decode inner Base64 to get Protobuf bytes
            innerBase64 = new string(innerBase64.Where(c => !char.IsWhiteSpace(c)).ToArray());
            byte[] protobufBytes = Convert.FromBase64String(innerBase64);

            if (protobufBytes.Length == 0)
            {
                return new List<Waypoint>();
            }

            // Step 4: Deserialize Protobuf to List<Waypoint>
            return DeserializeProtobuf(protobufBytes);
        }
        catch (InvalidDataException)
        {
            // Compression error - likely empty or invalid data
            return new List<Waypoint>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decompress waypoint data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Check if compressed data has custom 4-byte header before GZip
    /// </summary>
    private static bool HasGzipHeader(byte[] compressedBytes)
    {
        return compressedBytes.Length > 4 &&
               compressedBytes[4] == 0x1F &&
               compressedBytes[5] == 0x8B;
    }

    /// <summary>
    /// Decompress GZip layer and return inner Base64 string
    /// </summary>
    private static string DecompressGzipLayer(byte[] compressedBytes, int offset)
    {
        using (var inputMs = new MemoryStream(compressedBytes, offset, compressedBytes.Length - offset))
        using (var gzip = new GZipStream(inputMs, CompressionMode.Decompress))
        using (var reader = new StreamReader(gzip, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    /// <summary>
    /// Deserialize Protobuf bytes to List of Waypoint objects
    /// </summary>
    private static List<Waypoint> DeserializeProtobuf(byte[] protobufBytes)
    {
        using (var protoMs = new MemoryStream(protobufBytes))
        {
            return Serializer.Deserialize<List<Waypoint>>(protoMs) ?? new List<Waypoint>();
        }
    }

    /// <summary>
    /// Get waypoints for the last N hours
    /// </summary>
    public async Task<List<Waypoint>> GetVehicleWaypointsLastHoursAsync(
        string bearerToken,
        string vehicleId,
        int hours)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-hours);
        return await GetVehicleWaypointsAsync(bearerToken, vehicleId, startTime, endTime);
    }

    /// <summary>
    /// Get waypoints for a specific date (full day)
    /// </summary>
    public async Task<List<Waypoint>> GetVehicleWaypointsForDateAsync(
        string bearerToken,
        string vehicleId,
        DateTime date)
    {
        var startTime = date.Date; // Start of day
        var endTime = startTime.AddDays(1).AddSeconds(-1); // End of day
        return await GetVehicleWaypointsAsync(bearerToken, vehicleId, startTime, endTime);
    }
}
