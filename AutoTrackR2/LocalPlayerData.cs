namespace AutoTrackR2;


public enum GameMode
{
    Unknown,
    PersistentUniverse,
    ArenaCommander
}

public static class LocalPlayerData
{
    public static string? Username;
    public static string? PlayerShip;
    public static string? GameVersion;
    public static GameMode CurrentGameMode;
    public static string? LastSeenVehicleLocation;
}