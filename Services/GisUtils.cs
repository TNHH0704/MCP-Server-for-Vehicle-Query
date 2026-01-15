namespace McpVersionVer2.Services;

public class GisUtil
{
    /// <summary>
    ///     Get distance in meter for 2 lon/lats
    /// </summary>
    /// <param name="lon1"></param>
    /// <param name="lat1"></param>
    /// <param name="lon2"></param>
    /// <param name="lat2"></param>
    /// <returns></returns>
    public static int GetDistance(double lon1, double lat1, double lon2, double lat2)
    {
        try
        {
            const int
                EARTH_RADIUS_IN_KM
                    = 6378137; // mean radius of the earth (km) at 39 degrees from the equator

            var c = lat1 * Math.PI / 180;
            var a = lon1 * Math.PI / 180;
            var d = lat2 * Math.PI / 180;
            var b = lon2 * Math.PI / 180;

            return (int)(EARTH_RADIUS_IN_KM *
                         (2 *
                          Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((c - d) / 2), 2) +
                                              Math.Cos(c) * Math.Cos(d) *
                                              Math.Pow(Math.Sin((a - b) / 2), 2)))));
        }
        catch (Exception) { }

        return 0;
    }
}
