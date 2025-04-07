using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

public class JumpDriveStateChangedEvent : ILogEventHandler
{
    public Regex Pattern { get; }
    private Regex _cleanUpPattern = new Regex(@"(.+?)_\d+$");
    
    public JumpDriveStateChangedEvent()
    {
        Pattern = new Regex(@"<Jump Drive State Changed>.*.adam: (?<ShipName>.*.) in");
    }
    
    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;
        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;
        
        match = _cleanUpPattern.Match(match.Groups["ShipName"].Value);
        if (match.Success)
        {
            TrackREventDispatcher.OnJumpDriveStateChangedEvent(match.Groups[1].Value);;
        }
    }
}