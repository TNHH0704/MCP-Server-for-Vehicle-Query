public class WaypointSummary
{
    public string Timestamp { get; set; } = "";
    public int RawGpsTime { get; set; } 
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Altitude { get; set; }
    public double Speed { get; set; }
    public byte Heading { get; set; }
    public byte Satellites { get; set; }
    public double Mileage { get; set; }
    public double GpsMileage { get; set; }
    public double CumulativeDistanceKm { get; set; }
    public short EventId { get; set; }
    public int Status { get; set; }
    public double Voltage { get; set; }
    public short Battery { get; set; }
    public string? DriverId { get; set; }
    public string? DriverCode { get; set; }
    public string? Info { get; set; }
    public string VehicleStatus { get; set; } = ""; // "running", "idle", or "stop"
}