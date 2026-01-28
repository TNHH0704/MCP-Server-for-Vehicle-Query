namespace McpVersionVer2.Utils;

/// <summary>
/// Geographic Information System utilities for coordinate conversion and calculations
/// </summary>
public static class GisUtils
{
    /// <summary>
    /// Standard GPS coordinate divisor for converting stored coordinates to decimal degrees
    /// </summary>
    public const double GPS_COORDINATE_DIVISOR = 1_000_000.0;

    /// <summary>
    /// Alternative coordinate divisor used for some vehicle status data
    /// </summary>
    public const double VEHICLE_STATUS_COORDINATE_DIVISOR = 10_000.0;

    /// <summary>
    /// Convert GPS coordinates from stored format to decimal degrees
    /// </summary>
    /// <param name="x">Longitude coordinate in stored format</param>
    /// <param name="y">Latitude coordinate in stored format</param>
    /// <returns>Tuple of (latitude, longitude) in decimal degrees</returns>
    public static (double latitude, double longitude) ConvertGpsCoordinates(long x, long y)
    {
        return (y / GPS_COORDINATE_DIVISOR, x / GPS_COORDINATE_DIVISOR);
    }

    /// <summary>
    /// Convert GPS coordinates from stored format to decimal degrees (int version)
    /// </summary>
    /// <param name="x">Longitude coordinate in stored format</param>
    /// <param name="y">Latitude coordinate in stored format</param>
    /// <returns>Tuple of (latitude, longitude) in decimal degrees</returns>
    public static (double latitude, double longitude) ConvertGpsCoordinates(int x, int y)
    {
        return (y / GPS_COORDINATE_DIVISOR, x / GPS_COORDINATE_DIVISOR);
    }

    /// <summary>
    /// Convert vehicle status coordinates from stored format to decimal degrees
    /// </summary>
    /// <param name="x">Longitude coordinate in stored format</param>
    /// <param name="y">Latitude coordinate in stored format</param>
    /// <returns>Tuple of (latitude, longitude) in decimal degrees</returns>
    public static (double latitude, double longitude) ConvertVehicleStatusCoordinates(long x, long y)
    {
        return (y / VEHICLE_STATUS_COORDINATE_DIVISOR, x / VEHICLE_STATUS_COORDINATE_DIVISOR);
    }

    /// <summary>
    /// Check if GPS coordinates are valid (not 0,0)
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <param name="longitude">Longitude in decimal degrees</param>
    /// <returns>True if coordinates are valid</returns>
    public static bool IsValidCoordinate(double latitude, double longitude)
    {
        return latitude != 0 && longitude != 0;
    }

    /// <summary>
    /// Round a coordinate value to 6 decimal places
    /// </summary>
    /// <param name="value">Coordinate value to round</param>
    /// <returns>Rounded coordinate value</returns>
    public static double RoundCoordinateTo6Decimals(double value)
    {
        return Math.Round(value, 6);
    }

    /// <summary>
    /// Get distance in meters between two GPS coordinates using Haversine formula
    /// </summary>
    /// <param name="lon1">Longitude of first point in decimal degrees</param>
    /// <param name="lat1">Latitude of first point in decimal degrees</param>
    /// <param name="lon2">Longitude of second point in decimal degrees</param>
    /// <param name="lat2">Latitude of second point in decimal degrees</param>
    /// <returns>Distance in meters</returns>
    public static int GetDistance(double lon1, double lat1, double lon2, double lat2)
    {
        try
        {
            const int EARTH_RADIUS_IN_METERS = 6_378_137; // mean radius of the earth in meters

            var c = lat1 * Math.PI / 180;
            var a = lon1 * Math.PI / 180;
            var d = lat2 * Math.PI / 180;
            var b = lon2 * Math.PI / 180;

            return (int)(EARTH_RADIUS_IN_METERS *
                         (2 *
                          Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((c - d) / 2), 2) +
                                              Math.Cos(c) * Math.Cos(d) *
                                              Math.Pow(Math.Sin((a - b) / 2), 2)))));
        }
        catch (Exception)
        {
            return 0;
        }
    }
}