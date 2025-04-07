using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

public class InArenaCommanderEvent : ILogEventHandler
{
    public Regex Pattern { get; }
    
    public InArenaCommanderEvent()
    {
        Pattern = new Regex("Requesting Mode Change");
    }
    
    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;
        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;
        
        TrackREventDispatcher.OnPlayerChangedGameModeEvent(GameMode.ArenaCommander);
    }
}