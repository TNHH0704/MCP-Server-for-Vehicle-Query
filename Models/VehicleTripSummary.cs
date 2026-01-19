public class VehicleTripSummary
{
    public string VehicleId { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public double TotalDistanceKm { get; set; }
    public double DurationHours { get; set; }
    public double AverageSpeedKmh { get; set; }
    public double MaxSpeedKmh { get; set; }
    public int StopCount { get; set; }
    public int TotalWaypoints { get; set; }
    public int MovingWaypoints { get; set; }
    public double AmountOfTimeStop { get; set; } // Time spent stopped in hours
    public double AmountOfTimeRunning { get; set; } // Time spent running in hours
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public string StartInfo { get; set; } = "";
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }
    public string EndInfo { get; set; } = "";
}