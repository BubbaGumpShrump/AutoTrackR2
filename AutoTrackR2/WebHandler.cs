using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoTrackR2.LogEventHandlers;
using System.Globalization;

namespace AutoTrackR2;

public static class WebHandler
{
    class APIKillData
    {
        public string? victim_ship { get; set; }
        public string? victim { get; set; }
        public string? enlisted { get; set; }
        public string? rsi { get; set; }
        public string? weapon { get; set; }
        public string? method { get; set; }
        public string? loadout_ship { get; set; }
        public string? game_version { get; set; }
        public string? gamemode { get; set; }
        public string? trackr_version { get; set; }
        public string? location { get; set; }
        public long time { get; set; }
    }

    public static async Task<PlayerData?> GetPlayerData(string enemyPilot)
    {
        var joinDataPattern = new Regex("<span class=\"label\">Enlisted</span>\\s*<strong class=\"value\">([^<]+)</strong>");
        var ueePattern = new Regex("<p class=\"entry citizen-record\">\\n.*.<span class=\"label\">UEE Citizen Record<\\/span>\\n.*.<strong class=\"value\">#(?<UEERecord>\\d+)<\\/strong>");
        var orgPattern = new Regex("\\/orgs\\/(?<OrgURL>[A-z0-9]+)\" .*\\>(?<OrgName>.*)<");
        var pfpPattern = new Regex("/media/(.*)\"");

        // Make web request to check player data
        var playerData = new PlayerData();
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"https://robertsspaceindustries.com/en/citizens/{enemyPilot}");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var joinDataMatch = joinDataPattern.Match(content);
        if (joinDataMatch.Success)
        {
            playerData.JoinDate = joinDataMatch.Groups[1].Value;
        }

        var ueeMatch = ueePattern.Match(content);
        if (ueeMatch.Success)
        {
            playerData.UEERecord = ueeMatch.Groups["UEERecord"].Value == "n/a" ? "-1" : ueeMatch.Groups[1].Value;
        }

        var orgMatch = orgPattern.Match(content);
        if (orgMatch.Success)
        {
            playerData.OrgName = orgMatch.Groups["OrgName"].Value;
            playerData.OrgURL = "https://robertsspaceindustries.com/en/orgs/" + orgMatch.Groups["OrgURL"].Value;
        }

        var pfpMatch = pfpPattern.Match(content);
        if (pfpMatch.Success)
        {
            var match = pfpMatch.Groups[1].Value;
            if (match.Contains("heap_thumb"))
            {
                playerData.PFPURL = "https://cdn.robertsspaceindustries.com/static/images/account/avatar_default_big.jpg";
            }
            else
            {
                playerData.PFPURL = "https://robertsspaceindustries.com/media/" + pfpMatch.Groups[1].Value;
            }
        }

        return playerData;
    }

    public static async Task SubmitKill(KillData killData)
    {
        var apiKillData = new APIKillData
        {
            victim_ship = killData.EnemyShip,
            victim = killData.EnemyPilot,
            enlisted = killData.Enlisted,
            rsi = killData.RecordNumber,
            weapon = killData.Weapon,
            method = killData.Method,
            gamemode = killData.Mode,
            loadout_ship = killData.Ship,
            game_version = killData.GameVersion,
            trackr_version = killData.TrackRver,
            location = "Unknown",
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (string.IsNullOrEmpty(apiKillData.rsi))
        {
            apiKillData.rsi = "-1";
        }

        var httpClient = new HttpClient();
        string jsonData = JsonSerializer.Serialize(apiKillData);
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + ConfigManager.ApiKey);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "AutoTrackR2");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        Console.WriteLine("\n=== Kill Submission Debug Info ===");
        Console.WriteLine($"API URL: {ConfigManager.ApiUrl}register-kill");
        Console.WriteLine($"Victim: {apiKillData.victim}");
        Console.WriteLine($"Victim Ship: {apiKillData.victim_ship}");
        Console.WriteLine($"Weapon: {apiKillData.weapon}");
        Console.WriteLine($"Method: {apiKillData.method}");
        Console.WriteLine($"Game Mode: {apiKillData.gamemode}");
        Console.WriteLine($"Time (Unix): {apiKillData.time}");
        Console.WriteLine($"Time (UTC): {DateTimeOffset.UtcNow}");
        Console.WriteLine("=== End Debug Info ===\n");

        var response = await httpClient.PostAsync(ConfigManager.ApiUrl + "register-kill", new StringContent(jsonData, Encoding.UTF8, "application/json"));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine("Failed to submit kill data:");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");
            Console.WriteLine("Request Data:");
            Console.WriteLine(jsonData);
        }
        else if (response.StatusCode == HttpStatusCode.OK)
        {
            Console.WriteLine("Successfully submitted kill data");
            Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");
        }
    }
}