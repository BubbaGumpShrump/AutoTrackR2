using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

public struct VehicleDestructionData
{
    public string Vehicle { get; set; }
    public string VehicleZone { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public string Driver { get; set; }
    public int DestroyLevelFrom { get; set; }
    public int DestroyLevelTo { get; set; }
    public string CausedBy { get; set; }
    public string DamageType { get; set; }
}

public class VehicleDestructionEvent : ILogEventHandler
{
    public Regex Pattern { get; }
    
    public VehicleDestructionEvent()
    {
        Pattern = new Regex("""
                            "<(?<timestamp>[^>]+)> \[Notice\] <Vehicle Destruction> CVehicle::OnAdvanceDestroyLevel: " +
                            "Vehicle '(?<vehicle>[^']+)' \[\d+\] in zone '(?<vehicle_zone>[^']+)' " +
                            "\[pos x: (?<pos_x>[-\d\.]+), y: (?<pos_y>[-\d\.]+), z: (?<pos_z>[-\d\.]+) " +
                            "vel x: [^,]+, y: [^,]+, z: [^\]]+\] driven by '(?<driver>[^']+)' \[\d+\] " +
                            "advanced from destroy level (?<destroy_level_from>\d+) to (?<destroy_level_to>\d+) " +
                            "caused by '(?<caused_by>[^']+)' \[\d+\] with '(?<damage_type>[^']+)'"
                            """);
    }

    public void Handle(LogEntry entry)
    {
        var match = Pattern.Match(entry.Message);
        if (!match.Success)
        {
            return;
        }

        var data = new VehicleDestructionData
        {
            Vehicle = match.Groups["vehicle"].Value,
            VehicleZone = match.Groups["vehicle_zone"].Value,
            PosX = float.Parse(match.Groups["pos_x"].Value),
            PosY = float.Parse(match.Groups["pos_y"].Value),
            PosZ = float.Parse(match.Groups["pos_z"].Value),
            Driver = match.Groups["driver"].Value,
            DestroyLevelFrom = int.Parse(match.Groups["destroy_level_from"].Value),
            DestroyLevelTo = int.Parse(match.Groups["destroy_level_to"].Value),
            CausedBy = match.Groups["caused_by"].Value,
            DamageType = match.Groups["damage_type"].Value,
        };

        TrackREventDispatcher.OnVehicleDestructionEvent(data);
    }
}