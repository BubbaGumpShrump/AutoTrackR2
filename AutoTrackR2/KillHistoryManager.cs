using System.Globalization;
using System.IO;
using System.Text;

namespace AutoTrackR2;

public class KillHistoryManager
{
    private string _killHistoryPath;
    private readonly string _headers = "KillTime,EnemyPilot,EnemyShip,Enlisted,RecordNumber,OrgAffiliation,Player,Weapon,Ship,Method,Mode,GameVersion,TrackRver,Logged,PFP\n";
    
    public KillHistoryManager(string logPath)
    {
        _killHistoryPath = logPath;
        
        if (!File.Exists(_killHistoryPath))
        {
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
        
        // Append the new kill data to the CSV file
        var csv = new StringBuilder();
        csv.AppendLine($"\"{killData.KillTime}\",\"{killData.EnemyPilot}\",\"{killData.EnemyShip}\",\"{killData.Enlisted}\",\"{killData.RecordNumber}\",\"{killData.OrgAffiliation}\",\"{killData.Player}\",\"{killData.Weapon}\",\"{killData.Ship}\",\"{killData.Method}\",\"{killData.Mode}\",\"{killData.GameVersion}\",\"{killData.TrackRver}\",\"{killData.Logged}\",\"{killData.PFP}\"");
        File.AppendAllText(_killHistoryPath, csv.ToString());
    }
    
    public List<KillData> GetKills()
    {
        var kills = new List<KillData>();

        using var reader = new StreamReader(_killHistoryPath);
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
                PFP = data?[14]
            });
        }

        return kills;
    }

    public List<KillData> GetKillsInCurrentMonth()
    {
        string currentMonth = DateTime.Now.ToString("MMM", CultureInfo.InvariantCulture);
        var kills = GetKills();
        return kills.Where(kill => kill.KillTime?.Contains(currentMonth) == true).ToList();
    }
}