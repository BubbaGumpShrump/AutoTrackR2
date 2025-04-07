using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

public class GameVersionEvent : ILogEventHandler
{
    public Regex Pattern { get; }
    
    public GameVersionEvent()
    {
        Pattern = new Regex(@"--system-trace-env-id='pub-sc-alpha-(?<GameVersion>\d{3,4}-\d{7})'");
    }
    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;
        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;
        
        TrackREventDispatcher.OnGameVersionEvent(match.Groups["GameVersion"].Value);
    }
}