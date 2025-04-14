using System.Globalization;
using System.IO;
using System.Text;

namespace AutoTrackR2;

public class KillHistoryManager
{
    private string _killHistoryPath;
    private readonly string _headers = "KillTime,EnemyPilot,EnemyShip,Enlisted,RecordNumber,OrgAffiliation,Player,Weapon,Ship,Method,Mode,GameVersion,TrackRver,Logged,PFP,Hash\n";
    private readonly KillStreakManager _killStreakManager;

    public KillHistoryManager(string logPath, string soundsPath)
    {
        _killHistoryPath = logPath;
        _killStreakManager = new KillStreakManager(soundsPath);

        if (!File.Exists(_killHistoryPath))
        {
            File.WriteAllText(_killHistoryPath, _headers);
        }
        else
        {
            CheckAndFixMalformedCsv();
        }
    }

    private void CheckAndFixMalformedCsv()
    {
        try
        {
            // Try to read the file to check if it's malformed
            using var reader = new StreamReader(_killHistoryPath);
            var firstLine = reader.ReadLine();

            // If the file is empty or doesn't start with the correct headers, it's malformed
            if (string.IsNullOrEmpty(firstLine) || firstLine != _headers.TrimEnd('\n'))
            {
                // Create a backup of the malformed file
                string backupPath = Path.Combine(
                    Path.GetDirectoryName(_killHistoryPath)!,
                    "Kill-log.old"
                );

                // If Kill-log.old already exists, delete it
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                // Rename the malformed file
                File.Move(_killHistoryPath, backupPath);

                // Create a new file with correct headers
                File.WriteAllText(_killHistoryPath, _headers);
            }
        }
        catch (Exception ex)
        {
            // If there's any error reading the file, consider it malformed
            Console.WriteLine($"Error reading CSV file: {ex.Message}");
            string backupPath = Path.Combine(
                Path.GetDirectoryName(_killHistoryPath)!,
                "Kill-log.old"
            );

            // If Kill-log.old already exists, delete it
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            // Rename the malformed file
            File.Move(_killHistoryPath, backupPath);

            // Create a new file with correct headers
            File.WriteAllText(_killHistoryPath, _headers);
        }
    }

    public void AddKill(KillData killData)
    {
        // Ensure the CSV file exists
        // This should only happen if the file was deleted or corrupted
        if (!File.Exists(_killHistoryPath))
        {
            File.WriteAllText(_killHistoryPath, _headers);
        }
        
        // Remove comma from Enlisted
        killData.Enlisted = killData.Enlisted?.Replace(",", string.Empty);
        
        // Append the new kill data to the CSV file
        var csv = new StringBuilder();
        csv.AppendLine($"\"{killData.KillTime}\",\"{killData.EnemyPilot}\",\"{killData.EnemyShip}\",\"{killData.Enlisted}\",\"{killData.RecordNumber}\",\"{killData.OrgAffiliation}\",\"{killData.Player}\",\"{killData.Weapon}\",\"{killData.Ship}\",\"{killData.Method}\",\"{killData.Mode}\",\"{killData.GameVersion}\",\"{killData.TrackRver}\",\"{killData.Logged}\",\"{killData.PFP}\",\"{killData.Hash}\"");

        // Check file can be written to
        try
        {
            using var fileStream = new FileStream(_killHistoryPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fileStream);
            writer.Write(csv.ToString());

            // Trigger kill streak sound only if enabled
            if (ConfigManager.KillStreakEnabled == 1)
            {
                _killStreakManager.OnKill();
            }
        }
        catch (IOException ex)
        {
            // Handle the exception (e.g., log it)
            Console.WriteLine($"Error writing to file: {ex.Message}");
        }
    }

    public void ResetKillStreak()
    {
        _killStreakManager.OnDeath();
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

        return kills;
    }

    public List<KillData> GetKillsInCurrentMonth()
    {
        string currentMonth = DateTime.Now.ToString("MMM", CultureInfo.InvariantCulture);
        var kills = GetKills();

        // Because we are not using UTCNOW anymore and users already have kills saved, we need to make sure both formats are compatable. Otherwise people gonna delete their csv.
        return kills.Where(kill =>
        {
            if (string.IsNullOrEmpty(kill.KillTime)) return false;

            // Try to parse as Unix timestamp first
            if (long.TryParse(kill.KillTime, out long unixTime))
            {
                var date = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                return date.ToString("MMM", CultureInfo.InvariantCulture) == currentMonth;
            }

            // Fall back to checking if it contains the month name (old format)
            return kill.KillTime.Contains(currentMonth);
        }).ToList();
    }
}