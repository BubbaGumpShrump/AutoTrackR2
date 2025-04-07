namespace AutoTrackR2;

// Data returned from the CIG API
public struct PlayerData
{
    public string? PFPURL;
    public string? UEERecord;
    public string? OrgURL;
    public string? OrgName;
    public string? JoinDate;
}

// Amalgamation of all data from a single kill
public struct KillData
{
    public string? KillTime;
    public string? EnemyPilot;
    public string? EnemyShip;
    public string? Enlisted;
    public string? RecordNumber;
    public string? OrgAffiliation;
    public string? Player;
    public string? Weapon;
    public string? Ship;
    public string? Method;
    public string? Mode;
    public string? GameVersion;
    public string? TrackRver;
    public string? Logged;
    public string? PFP;
}