using System.Text.RegularExpressions;
using AutoTrackR2.Constants;

namespace AutoTrackR2.LogEventHandlers;

public struct VehicleDestructionData
{
    public string VehicleName { get; set; }
    public string VehicleId { get; set; }
    public string Team { get; set; }
    public string VehicleZone { get; set; }
    public string? Ship { get; set; }
}

public class VehicleDestructionEvent : ILogEventHandler
{
    public Regex Pattern { get; }
    private Regex _shipManufacturerPattern;
    private Regex _cleanUpPattern = new Regex(@"(.+?)_\d+$");

    public VehicleDestructionEvent()
    {
        Pattern = new Regex(@"<Vehicle Destruction> Vehicle '(?<vehicle_name>[^']+)' \[(?<vehicle_id>\d+)\] \[(?<team>[^\]]+)\] in zone '(?<vehicle_zone>[^']+)' has been destroyed");
        _shipManufacturerPattern = new Regex($"^({string.Join("|", ShipManufacturers.List)})");
    }

    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;

        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;

        var data = new VehicleDestructionData
        {
            VehicleName = match.Groups["vehicle_name"].Value,
            VehicleId = match.Groups["vehicle_id"].Value,
            Team = match.Groups["team"].Value,
            VehicleZone = match.Groups["vehicle_zone"].Value,
        };

        // Extract ship name from vehicle name if it's a ship
        var shipMatch = _shipManufacturerPattern.Match(data.VehicleName);
        if (shipMatch.Success)
        {
            var cleanMatch = _cleanUpPattern.Match(data.VehicleName);
            if (cleanMatch.Success)
            {
                data.Ship = cleanMatch.Groups[1].Value;
            }
        }

        TrackREventDispatcher.OnVehicleDestructionEvent(data);
    }
}