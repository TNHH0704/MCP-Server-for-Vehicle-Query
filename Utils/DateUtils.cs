namespace McpVersionVer2.Utils;

/// <summary>
/// Utility class for date and time operations, including GPS epoch conversions.
/// Centralizes date formatting and epoch handling to reduce duplication.
/// </summary>
public static class DateUtils
{
    /// <summary>
    /// GPS epoch used for vehicle tracking systems (January 1, 2010, 00:00:00 Local).
    /// </summary>
    public static readonly DateTime GpsEpoch = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Local);

    /// <summary>
    /// Converts GPS time seconds since epoch to DateTime.
    /// </summary>
    /// <param name="gpsTimeSeconds">Seconds since GPS epoch.</param>
    /// <returns>DateTime in Local kind.</returns>
    public static DateTime FromGpsEpoch(int gpsTimeSeconds)
    {
        return GpsEpoch.AddSeconds(gpsTimeSeconds);
    }

    /// <summary>
    /// Converts DateTime to GPS time seconds since epoch.
    /// </summary>
    /// <param name="dateTime">DateTime to convert.</param>
    /// <returns>Seconds since GPS epoch.</returns>
    public static int ToGpsEpoch(DateTime dateTime)
    {
        return (int)(dateTime - GpsEpoch).TotalSeconds;
    }

    /// <summary>
    /// Ensures DateTime is in UTC kind, converting if necessary.
    /// </summary>
    /// <param name="dateTime">DateTime to ensure is UTC.</param>
    /// <returns>DateTime in UTC kind.</returns>
    public static DateTime EnsureUtc(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
    }

    /// <summary>
    /// Standard date format used for API responses.
    /// </summary>
    public const string StandardFormat = "dd-MM-yyyy HH:mm:ss";

    /// <summary>
    /// Formats DateTime to standard API format.
    /// </summary>
    /// <param name="dateTime">DateTime to format.</param>
    /// <returns>Formatted string.</returns>
    public static string FormatForApi(DateTime dateTime)
    {
        return dateTime.ToString(StandardFormat);
    }

    /// <summary>
    /// Formats DateTime to standard API format with UTC suffix.
    /// </summary>
    /// <param name="dateTime">DateTime to format.</param>
    /// <returns>Formatted string with UTC suffix.</returns>
    public static string FormatForApiUtc(DateTime dateTime)
    {
        return dateTime.ToString(StandardFormat) + " UTC";
    }

    /// <summary>
    /// Standard date-only format used for API responses.
    /// </summary>
    public const string DateOnlyFormat = "dd-MM-yyyy";

    /// <summary>
    /// Formats DateTime to date-only format.
    /// </summary>
    /// <param name="dateTime">DateTime to format.</param>
    /// <returns>Formatted date string.</returns>
    public static string FormatDateOnly(DateTime dateTime)
    {
        return dateTime.ToString(DateOnlyFormat);
    }
}