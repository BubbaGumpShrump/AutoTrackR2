using System.Text.RegularExpressions;
using AutoTrackR2.Constants;

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
    private Regex _cleanUpPattern = new Regex(@"^(.+?)_\d+$");
    private Regex _shipManufacturerPattern;
    private string _lastKillShip = string.Empty;

    public ActorDeathEvent()
    {
        Pattern = new Regex(@"<(?<Timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z)> \[Notice\] <Actor Death> CActor::Kill: '(?<EnemyPilot>[^']+)' \[\d+\] in zone '(?<EnemyShip>[^']+)' killed by '(?<Player>[^']+)' \[[^']+\] using '(?<Weapon>[^']+)' \[Class (?<Class>[^\]]+)\] with damage type '(?<DamageType>[^']+)");
        _shipManufacturerPattern = new Regex($"^({string.Join("|", ShipManufacturers.List)})");
    }

    private bool IsValidShip(string shipName)
    {
        // Clean up the ship name first
        if (_cleanUpPattern.IsMatch(shipName))
        {
            shipName = _cleanUpPattern.Match(shipName).Groups[1].Value;
        }

        // A valid ship must start with a known manufacturer
        return _shipManufacturerPattern.IsMatch(shipName);
    }

    public void Handle(LogEntry entry)
    {
        if (entry.Message is null) return;

        var match = Pattern.Match(entry.Message);
        if (!match.Success) return;

        var data = new ActorDeathData
        {
            VictimPilot = match.Groups["EnemyPilot"].Value,
            VictimShip = match.Groups["EnemyShip"].Value,
            Player = match.Groups["Player"].Value,
            Weapon = match.Groups["Weapon"].Value,
            Class = match.Groups["Class"].Value,
            DamageType = match.Groups["DamageType"].Value,
            Timestamp = match.Groups["Timestamp"].Value
        };

        // Clean up weapon name
        if (_cleanUpPattern.IsMatch(data.Weapon))
        {
            data.Weapon = _cleanUpPattern.Match(data.Weapon).Groups[1].Value;
        }

        // First check if this is a valid ship
        if (!IsValidShip(data.VictimShip))
        {
            data.VictimShip = "Player";
        }
        else
        {
            // For valid ships, check for passenger flag
            if (data.VictimShip == _lastKillShip)
            {
                data.VictimShip = "Passenger";
            }
            else
            {
                _lastKillShip = data.VictimShip;
            }
        }
        
        
        TrackREventDispatcher.OnActorDeathEvent(data);
    }
}