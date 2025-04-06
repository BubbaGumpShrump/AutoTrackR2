using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

public struct InstancedInteriorData
{
    public string Entity;
    public string OwnerGEID;
    public string ManagerGEID;
    public string InstancedInterior;
    public string? Ship;
}

// A ship loadout has been changed
public class InstancedInteriorEvent : ILogEventHandler
{
    public Regex Pattern { get; }

    private Regex _shipManufacturerPattern;
    private Regex _cleanUpPattern = new Regex(@"(.+?)_\d+$");
    
    private List<string> _shipManufacturers = new List<string>
    {
        "ORIG",
        "CRUS",
        "RSI",
        "AEGS",
        "VNCL",
        "DRAK",
        "ANVL",
        "BANU",
        "MISC",
        "CNOU",
        "XIAN",
        "GAMA",
        "TMBL",
        "ESPR",
        "KRIG",
        "GRIN",
        "XNAA",
        "MRAI"
    };
    
    public InstancedInteriorEvent()
    {
        Pattern = new Regex(@"\[InstancedInterior\] OnEntityLeaveZone - InstancedInterior \[(?<InstancedInterior>[^\]]+)\] \[\d+\] -> Entity \[(?<Entity>[^\]]+)\] \[\d+\] -- m_openDoors\[\d+\], m_managerGEID\[(?<ManagerGEID>\d+)\], m_ownerGEID\[(?<OwnerGEID>[^\[]+)\]");
        _shipManufacturerPattern = new Regex($"^({string.Join("|", _shipManufacturers)})");
    }

    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;
        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;
        
        var data = new InstancedInteriorData {
            Entity = match.Groups["Entity"].Value,
            OwnerGEID = match.Groups["OwnerGEID"].Value,
            ManagerGEID = match.Groups["ManagerGEID"].Value,
            InstancedInterior = match.Groups["InstancedInterior"].Value,
        };
        
        match = _shipManufacturerPattern.Match(data.Entity);
        if (match.Success)
        {
            match = _cleanUpPattern.Match(data.Entity);
            if (match.Success)
            {
                data.Ship = match.Groups[1].Value;
            }
        }
        
        TrackREventDispatcher.OnInstancedInteriorEvent(data);
        
    }

}