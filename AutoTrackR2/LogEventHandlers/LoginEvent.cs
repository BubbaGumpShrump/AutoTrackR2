using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

// Local player has logged in
public class LoginEvent : ILogEventHandler
{
    public Regex Pattern { get; }
    
    public LoginEvent()
    {
        Pattern = new Regex(@"\[Notice\] <Legacy login response> \[CIG-net\] User Login Success - Handle\[(?<Player>[A-Za-z0-9_-]+)\]");
    }
    
    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;
        
        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;

        TrackREventDispatcher.OnPlayerLoginEvent(match.Groups["Player"].Value);
    }
}