using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AutoTrackR2;

public class KillHistoryManager
{
    private readonly string _dbPath;
    private readonly KillStreakManager _killStreakManager;
    private readonly ConcurrentQueue<KillData> _killQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _processingTask;
    private bool _killStreakSoundEnabled = true;

    public KillHistoryManager(string logPath, string soundsPath)
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoTrackR2");
        Directory.CreateDirectory(appDataPath); // Ensure the directory exists
        _dbPath = Path.Combine(appDataPath, "kills.db");

        InitializeDatabase();

        _killStreakManager = new KillStreakManager(soundsPath);
        _killQueue = new ConcurrentQueue<KillData>();
        _cancellationTokenSource = new CancellationTokenSource();

        // Start the background processing task
        _processingTask = Task.Run(ProcessKillQueue);
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS kills (
                Hash TEXT PRIMARY KEY,
                KillTime TEXT,
                EnemyPilot TEXT,
                EnemyShip TEXT,
                Enlisted TEXT,
                RecordNumber TEXT,
                OrgAffiliation TEXT,
                Weapon TEXT,
                Ship TEXT,
                Method TEXT,
                Location TEXT,
                Mode TEXT,
                GameVersion TEXT,
                TrackRver TEXT,
                Logged TEXT,
                PFP TEXT
            );
        ";
        command.ExecuteNonQuery();
    }

    private async Task ProcessKillQueue()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                if (_killQueue.TryDequeue(out var kill))
                {
                    await ProcessKillAsync(kill);
                }
                else
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing kill: {ex.Message}");
                await Task.Delay(1000, _cancellationTokenSource.Token);
            }
        }
    }

    private async Task ProcessKillAsync(KillData kill)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO kills (
                    Hash, KillTime, EnemyPilot, EnemyShip, Enlisted, RecordNumber, OrgAffiliation, Weapon, Ship, Method, Location, Mode, GameVersion, TrackRver, Logged, PFP
                ) VALUES (
                    $Hash, $KillTime, $EnemyPilot, $EnemyShip, $Enlisted, $RecordNumber, $OrgAffiliation, $Weapon, $Ship, $Method, $Location, $Mode, $GameVersion, $TrackRver, $Logged, $PFP
                );
            ";
            command.Parameters.AddWithValue("$Hash", kill.Hash ?? "");
            command.Parameters.AddWithValue("$KillTime", kill.KillTime ?? "");
            command.Parameters.AddWithValue("$EnemyPilot", kill.EnemyPilot ?? "");
            command.Parameters.AddWithValue("$EnemyShip", kill.EnemyShip ?? "");
            command.Parameters.AddWithValue("$Enlisted", kill.Enlisted ?? "");
            command.Parameters.AddWithValue("$RecordNumber", kill.RecordNumber ?? "");
            command.Parameters.AddWithValue("$OrgAffiliation", kill.OrgAffiliation ?? "");
            command.Parameters.AddWithValue("$Weapon", kill.Weapon ?? "");
            command.Parameters.AddWithValue("$Ship", kill.Ship ?? "");
            command.Parameters.AddWithValue("$Method", kill.Method ?? "");
            command.Parameters.AddWithValue("$Location", kill.Location ?? "");
            command.Parameters.AddWithValue("$Mode", kill.Mode ?? "");
            command.Parameters.AddWithValue("$GameVersion", kill.GameVersion ?? "");
            command.Parameters.AddWithValue("$TrackRver", kill.TrackRver ?? "");
            command.Parameters.AddWithValue("$Logged", kill.Logged ?? "");
            command.Parameters.AddWithValue("$PFP", kill.PFP ?? "");
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing kill to SQLite: {ex.Message}");
            throw;
        }
    }

    public void AddKill(KillData kill)
    {
        _killQueue.Enqueue(kill);
    }

    public void PlayKillStreakSound()
    {
        _killStreakManager.OnKill();
    }

    public void ResetKillStreak()
    {
        _killStreakManager.OnDeath();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _processingTask.Wait();
        _cancellationTokenSource.Dispose();
    }

    public List<KillData> GetKills()
    {
        var kills = new List<KillData>();
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Hash, KillTime, EnemyPilot, EnemyShip, Enlisted, RecordNumber, OrgAffiliation, Weapon, Ship, Method, Location, Mode, GameVersion, TrackRver, Logged, PFP FROM kills";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            kills.Add(new KillData
            {
                Hash = reader.GetString(0),
                KillTime = reader.GetString(1),
                EnemyPilot = reader.GetString(2),
                EnemyShip = reader.GetString(3),
                Enlisted = reader.GetString(4),
                RecordNumber = reader.GetString(5),
                OrgAffiliation = reader.GetString(6),
                Weapon = reader.GetString(7),
                Ship = reader.GetString(8),
                Method = reader.GetString(9),
                Location = reader.GetString(10),
                Mode = reader.GetString(11),
                GameVersion = reader.GetString(12),
                TrackRver = reader.GetString(13),
                Logged = reader.GetString(14),
                PFP = reader.GetString(15)
            });
        }
        // Apply KillFeedLimit if specified
        if (ConfigManager.KillFeedLimit.HasValue && ConfigManager.KillFeedLimit.Value > 0)
        {
            kills = kills.TakeLast(ConfigManager.KillFeedLimit.Value).ToList();
        }
        return kills;
    }

    public List<KillData> GetKillsInCurrentMonth()
    {
        string currentMonth = DateTime.Now.ToString("MMM", CultureInfo.InvariantCulture);
        var kills = new List<KillData>();
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Hash, KillTime, EnemyPilot, EnemyShip, Enlisted, RecordNumber, OrgAffiliation, Weapon, Ship, Method, Location, Mode, GameVersion, TrackRver, Logged, PFP FROM kills";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var kill = new KillData
            {
                Hash = reader.GetString(0),
                KillTime = reader.GetString(1),
                EnemyPilot = reader.GetString(2),
                EnemyShip = reader.GetString(3),
                Enlisted = reader.GetString(4),
                RecordNumber = reader.GetString(5),
                OrgAffiliation = reader.GetString(6),
                Weapon = reader.GetString(7),
                Ship = reader.GetString(8),
                Method = reader.GetString(9),
                Location = reader.GetString(10),
                Mode = reader.GetString(11),
                GameVersion = reader.GetString(12),
                TrackRver = reader.GetString(13),
                Logged = reader.GetString(14),
                PFP = reader.GetString(15)
            };
            // Check if the kill is from the current month before adding it
            if (!string.IsNullOrEmpty(kill.KillTime))
            {
                if (long.TryParse(kill.KillTime, out long unixTime))
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                    if (date.ToString("MMM", CultureInfo.InvariantCulture) == currentMonth)
                    {
                        kills.Add(kill);
                    }
                }
            }
        }
        return kills;
    }
}