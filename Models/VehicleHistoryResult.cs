using System;
using McpVersionVer2.Services;

namespace McpVersionVer2.Models;

public class VehicleHistoryResult
{
    public string VehicleId { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalWaypoints { get; set; }
    public int MovingWaypoints { get; set; }
    public List<WaypointSummary> Waypoints { get; set; } = new();
    public int? HoursBack { get; set; }
    public string? Date { get; set; }

    // Trip Statistics
    public double TotalDistanceKm { get; set; }
    public string TotalRunningTime { get; set; } = "";
    public string TotalStopTime { get; set; } = "";
    public double TotalRunningTimeHours { get; set; }
    public double TotalStopTimeHours { get; set; }
    public string TotalRunningTimeFormatted { get; set; } = "";
    public string TotalStopTimeFormatted { get; set; } = "";
    public int AmountOfTimeStop { get; set; }
    public double AverageSpeedKmh { get; set; }
    public double HighestSpeedKmh { get; set; }
}
