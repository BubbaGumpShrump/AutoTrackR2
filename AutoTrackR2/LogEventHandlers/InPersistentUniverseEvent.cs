using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

public class InPersistentUniverseEvent : ILogEventHandler
{
    public Regex Pattern { get; }
    
    public InPersistentUniverseEvent()
    {
        Pattern = new Regex(@"<\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z> \[Notice\] <ContextEstablisherTaskFinished> establisher=""CReplicationModel"" message=""CET completed"" taskname=""StopLoadingScreen"" state=[^\s()]+\(\d+\) status=""Finished"" runningTime=\d+\.\d+ numRuns=\d+ map=""megamap"" gamerules=""SC_Default"" sessionId=""[a-f0-9\-]+"" \[Team_Network\]\[Network\]\[Replication\]\[Loading\]\[Persistence\]");
    }
    
    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;
        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;
        
        TrackREventDispatcher.OnPlayerChangedGameModeEvent(GameMode.PersistentUniverse);
    }
}