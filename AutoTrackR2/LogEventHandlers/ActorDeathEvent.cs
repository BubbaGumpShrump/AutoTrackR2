using System.Text.RegularExpressions;
using AutoTrackR2.Constants;
using AutoTrackR2;

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
    private Regex _shipManufacturerPattern;
    private string _lastKillShip = string.Empty;
    private Regex cleanUpPattern = new Regex(@"^(.+?)(?:_\d+)*$");

    public ActorDeathEvent()
    {
        Pattern = new Regex(@"<(?<Timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z)> \[Notice\] <Actor Death> CActor::Kill: '(?<EnemyPilot>[^']+)' \[\d+\] in zone '(?<EnemyShip>[^']+)' killed by '(?<Player>[^']+)' \[[^']+\] using '(?<Weapon>[^']+)' \[Class (?<Class>[^\]]+)\] with damage type '(?<DamageType>[^']+)");
        _shipManufacturerPattern = new Regex($"^({string.Join("|", ShipManufacturers.List)})");
    }

    private bool IsValidShip(string shipName)
    {
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

        // Check if the damage type is TakeDown or Melee
        if (data.DamageType == "TakeDown" || data.DamageType == "Melee")
        {
            LocalPlayerData.PlayerShip = "Player";
        }

        // Check if the weapon is in our list of weapons
        if (Weapons.List.Contains(data.Weapon))
        {
            LocalPlayerData.PlayerShip = "Player";
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

        // Clean up ship and weapon names (only if not set to Player or Passenger)
        if (data.VictimShip != "Player" && data.VictimShip != "Passenger" && cleanUpPattern.IsMatch(data.VictimShip))
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