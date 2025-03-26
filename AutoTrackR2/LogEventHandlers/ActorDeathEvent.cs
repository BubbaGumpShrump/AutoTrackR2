using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

public struct ActorDeathData
{
    public string VictimPilot;
    public string VictimShip;
    public string Player;
    public string Weapon;
    public string Class;
    public string DamageType;
    public string Timestamp;
}

public class ActorDeathEvent : ILogEventHandler
{
    public Regex Pattern { get; }
    public ActorDeathEvent()
    {
        Pattern = new Regex(@"<Actor Death> CActor::Kill: '(?<EnemyPilot>[^']+)' \[\d+\] in zone '(?<EnemyShip>[^']+)' killed by '(?<Player>[^']+)' \[[^']+\] using '(?<Weapon>[^']+)' \[Class (?<Class>[^\]]+)\] with damage type '(?<DamageType>[^']+)");
    }
    
    Regex cleanUpPattern = new Regex(@"^(.+?)_\d+$");

    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;
        
        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;
        
        var data = new ActorDeathData {
            VictimPilot = match.Groups["EnemyPilot"].Value,
            VictimShip = match.Groups["EnemyShip"].Value,
            Player = match.Groups["Player"].Value,
            Weapon = match.Groups["Weapon"].Value,
            Class = match.Groups["Class"].Value,
            DamageType = match.Groups["DamageType"].Value,
            Timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        if (cleanUpPattern.IsMatch(data.VictimShip))
        {
            data.VictimShip = cleanUpPattern.Match(data.VictimShip).Groups[1].Value;
        }
        
        if (cleanUpPattern.IsMatch(data.Weapon))
        {
            data.Weapon = cleanUpPattern.Match(data.Weapon).Groups[1].Value;
        }
        
        
        TrackREventDispatcher.OnActorDeathEvent(data);
        
    }


}