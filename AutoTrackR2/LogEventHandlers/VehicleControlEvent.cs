using System.Text.RegularExpressions;
using AutoTrackR2.Constants;

namespace AutoTrackR2.LogEventHandlers;

public struct VehicleControlData
{
  public string Action { get; set; } // "SetDriver" or "ClearDriver"
  public string VehicleName { get; set; }
  public string? Ship { get; set; }
}

public class VehicleControlEvent : ILogEventHandler
{
  public Regex Pattern { get; }
  private Regex _cleanUpPattern = new Regex(@"(.+?)_\d+$");

  public VehicleControlEvent()
  {
    // Pattern for entering vehicle (granted control)
    Pattern = new Regex(@"granted control token for '(?<vehicle_name>[^']+)'", RegexOptions.Compiled);
  }

  public void Handle(LogEntry entry)
  {
    if (entry.Message is null) return;

    // Check for vehicle entry (granted control)
    var enterMatch = Pattern.Match(entry.Message);
    if (enterMatch.Success)
    {
      var vehicleName = enterMatch.Groups["vehicle_name"].Value;
      var cleanedShipName = CleanShipName(vehicleName);
      LocalPlayerData.PlayerShip = cleanedShipName;
      var data = new VehicleControlData
      {
        Action = "SetDriver",
        VehicleName = vehicleName,
        Ship = cleanedShipName
      };
      TrackREventDispatcher.OnVehicleControlEvent(data);
      return;
    }
  }

  private string CleanShipName(string vehicleName)
  {
    var cleanMatch = _cleanUpPattern.Match(vehicleName);
    return cleanMatch.Success ? cleanMatch.Groups[1].Value : vehicleName;
  }
}

public class VehicleControlClearEvent : ILogEventHandler
{
  public Regex Pattern { get; }

  public VehicleControlClearEvent()
  {
    // Pattern for exiting vehicle (releasing control)
    Pattern = new Regex(@"releasing control token", RegexOptions.Compiled);
  }

  public void Handle(LogEntry entry)
  {
    if (entry.Message is null) return;

    var match = Pattern.Match(entry.Message);
    if (match.Success)
    {
      LocalPlayerData.PlayerShip = "Player";
      var data = new VehicleControlData
      {
        Action = "ClearDriver",
        VehicleName = "Player",
        Ship = "Player"
      };
      TrackREventDispatcher.OnVehicleControlEvent(data);
    }
  }
}