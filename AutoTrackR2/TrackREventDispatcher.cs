using AutoTrackR2.LogEventHandlers;

namespace AutoTrackR2;

public static class TrackREventDispatcher
{
    // Local Player Login
    public static event Action<string>? PlayerLoginEvent;
    public static void OnPlayerLoginEvent(string playerName)
    {
        PlayerLoginEvent?.Invoke(playerName);
    }

    // An instanced interior has changed
    // Example: Player enters/leaves a ship
    public static event Action<InstancedInteriorData>? InstancedInteriorEvent;
    public static void OnInstancedInteriorEvent(InstancedInteriorData data)
    {
        InstancedInteriorEvent?.Invoke(data);
    }
    
    // Player changed GameMode (AC or PU)
    public static event Action<GameMode>? PlayerChangedGameModeEvent;
    public static void OnPlayerChangedGameModeEvent(GameMode mode)
    {
        PlayerChangedGameModeEvent?.Invoke(mode);
    }
    
    // Game version has been detected
    public static event Action<string>? GameVersionEvent;
    public static void OnGameVersionEvent(string value)
    {
        GameVersionEvent?.Invoke(value);
    }
    
    // Actor has died
    public static event Action<ActorDeathData>? ActorDeathEvent;
    public static void OnActorDeathEvent(ActorDeathData data)
    {
        ActorDeathEvent?.Invoke(data);
    }
    
    // Vehicle has been destroyed
    public static event Action<VehicleDestructionData>? VehicleDestructionEvent;
    public static void OnVehicleDestructionEvent(VehicleDestructionData data)
    {
        VehicleDestructionEvent?.Invoke(data);
    }
}