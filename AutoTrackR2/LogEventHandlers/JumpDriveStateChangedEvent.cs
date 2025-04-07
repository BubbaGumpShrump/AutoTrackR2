using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

public struct JumpDriveStateChangedData
{
    public string ShipName { get; set; }
    public string Location { get; set; }
}

public class JumpDriveStateChangedEvent : ILogEventHandler
{
    public Regex Pattern { get; }
    private Regex _cleanUpPattern = new Regex(@"(.+?)_\d+$");
    
    public JumpDriveStateChangedEvent()
    {
        Pattern = new Regex(@"<Jump Drive State Changed>.*.adam: (?<ShipName>.*.) in zone (?<Location>.*.)\)");
    }
    
    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;
        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;

        var data = new JumpDriveStateChangedData
        {
            Location = match.Groups["Location"].Value
        };

        match = _cleanUpPattern.Match(match.Groups["ShipName"].Value);
        if (match.Success)
        {
            data.ShipName = match.Groups[1].Value;
        }
        if (!string.IsNullOrEmpty(data.ShipName) && !string.IsNullOrEmpty(data.Location))
        {
            TrackREventDispatcher.OnJumpDriveStateChangedEvent(data);
        }
    }
}