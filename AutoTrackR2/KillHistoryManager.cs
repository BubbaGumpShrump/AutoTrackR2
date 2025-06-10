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
    private readonly string _killHistoryPath;
    private readonly string _headers = "KillTime,EnemyPilot,EnemyShip,Enlisted,RecordNumber,OrgAffiliation,Player,Weapon,Ship,Method,Mode,GameVersion,TrackRver,Logged,PFP,Hash\n";
    private readonly KillStreakManager _killStreakManager;
    private readonly ConcurrentQueue<KillData> _killQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _processingTask;
    private bool _killStreakSoundEnabled = true;

    public KillHistoryManager(string logPath, string soundsPath)
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoTrackR2");
        Directory.CreateDirectory(appDataPath); // Ensure the directory exists
        _killHistoryPath = Path.Combine(appDataPath, "Kill-log.csv");

        // Create the CSV file with headers if it doesn't exist
        if (!File.Exists(_killHistoryPath))
        {
            File.WriteAllText(_killHistoryPath, _headers);
        }

        _killStreakManager = new KillStreakManager(soundsPath);
        _killQueue = new ConcurrentQueue<KillData>();
        _cancellationTokenSource = new CancellationTokenSource();

        // Start the background processing task
        _processingTask = Task.Run(ProcessKillQueue);
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
            // Ensure all fields are properly escaped for CSV
            var fields = new[]
            {
                kill.KillTime.ToString(),
                EscapeCsvField(kill.EnemyPilot),
                EscapeCsvField(kill.EnemyShip),
                EscapeCsvField(kill.Enlisted),
                EscapeCsvField(kill.RecordNumber),
                EscapeCsvField(kill.OrgAffiliation),
                EscapeCsvField(kill.Player),
                EscapeCsvField(kill.Weapon),
                EscapeCsvField(kill.Ship),
                EscapeCsvField(kill.Method),
                EscapeCsvField(kill.Mode),
                EscapeCsvField(kill.GameVersion),
                EscapeCsvField(kill.TrackRver),
                EscapeCsvField(kill.Logged),
                EscapeCsvField(kill.PFP),
                EscapeCsvField(kill.Hash)
            };

            var csvLine = string.Join(",", fields);

            // Use FileShare.Read to allow other processes to read while we write
            using var stream = new FileStream(_killHistoryPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);
            await writer.WriteLineAsync(csvLine);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing kill to CSV: {ex.Message}");
            throw;
        }
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";

        // If the field contains any special characters, wrap it in quotes
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
        {
            // Double up any quotes
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }

        return field;
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

        using var reader = new StreamReader(new FileStream(_killHistoryPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        reader.ReadLine(); // Skip headers

        while (reader.Peek() >= 0)
        {
            var line = reader.ReadLine();

            // Remove extra quotes from CSV data
            // Todo: These quotes are for handling commas in the data, but not sure if they're necessary
            line = line?.Replace("\"", string.Empty);

            var data = line?.Split(',');

            kills.Add(new KillData
            {
                KillTime = data?[0],
                EnemyPilot = data?[1],
                EnemyShip = data?[2],
                Enlisted = data?[3],
                RecordNumber = data?[4],
                OrgAffiliation = data?[5],
                Player = data?[6],
                Weapon = data?[7],
                Ship = data?[8],
                Method = data?[9],
                Mode = data?[10],
                GameVersion = data?[11],
                TrackRver = data?[12],
                Logged = data?[13],
                PFP = data?[14],
                Hash = data?[15]
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

        // Read all kills directly from file, ignoring KillFeedLimit
        using var reader = new StreamReader(new FileStream(_killHistoryPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        reader.ReadLine(); // Skip headers

        while (reader.Peek() >= 0)
        {
            var line = reader.ReadLine();

            // Remove extra quotes from CSV data
            line = line?.Replace("\"", string.Empty);

            var data = line?.Split(',');

            // Check if the kill is from the current month before adding it
            var killTime = data?[0];
            if (string.IsNullOrEmpty(killTime)) continue;

            // Try to parse as Unix timestamp first
            if (long.TryParse(killTime, out long unixTime))
            {
                var date = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                if (date.ToString("MMM", CultureInfo.InvariantCulture) != currentMonth) continue;
            }
            else if (!killTime.Contains(currentMonth))
            {
                // Fall back to checking if it contains the month name (old format)
                continue;
            }

            kills.Add(new KillData
            {
                KillTime = killTime,
                EnemyPilot = data?[1],
                EnemyShip = data?[2],
                Enlisted = data?[3],
                RecordNumber = data?[4],
                OrgAffiliation = data?[5],
                Player = data?[6],
                Weapon = data?[7],
                Ship = data?[8],
                Method = data?[9],
                Mode = data?[10],
                GameVersion = data?[11],
                TrackRver = data?[12],
                Logged = data?[13],
                PFP = data?[14],
                Hash = data?[15]
            });
        }

        return kills;
    }
}