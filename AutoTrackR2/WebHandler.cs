using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoTrackR2.LogEventHandlers;
using System.Globalization;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace AutoTrackR2;

public static class WebHandler
{
    private static HashSet<string> _recordedKillHashes = new HashSet<string>();

    public static bool IsDuplicateKill(string hash)
    {
        return _recordedKillHashes.Contains(hash);
    }

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
        public string hash { get; set; } = string.Empty;
    }

    public static string GenerateKillHash(string victimName, long timestamp)
    {
        // Combine victim name and timestamp
        string combined = $"{victimName}_{timestamp}";

        // Create SHA256 hash
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));

            // Convert byte array to hex string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
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
        var timestamp = long.Parse(killData.KillTime!);
        var hash = GenerateKillHash(killData.EnemyPilot!, timestamp);

        // Check if this kill has already been recorded
        if (_recordedKillHashes.Contains(hash))
        {
            Console.WriteLine("Duplicate kill detected, skipping...");
            return;
        }

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
            location = killData.Location,
            time = timestamp,
            hash = hash
        };

        if (string.IsNullOrEmpty(apiKillData.rsi))
        {
            apiKillData.rsi = "-1";
        }

        if (!apiKillData.enlisted.Contains(","))
        {
            //Get second whitespace in string
            var index = apiKillData.enlisted.IndexOf(" ", apiKillData.enlisted.IndexOf(" ", StringComparison.Ordinal) + 1, StringComparison.Ordinal);
            if (index != -1)
            {
                apiKillData.enlisted = apiKillData.enlisted.Insert(index, ",");
            }
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
        Console.WriteLine($"Location: {apiKillData.location}");
        Console.WriteLine($"Weapon: {apiKillData.weapon}");
        Console.WriteLine($"Method: {apiKillData.method}");
        Console.WriteLine($"Game Mode: {apiKillData.gamemode}");
        Console.WriteLine($"Time (Unix): {apiKillData.time}");
        Console.WriteLine($"Time (UTC): {DateTimeOffset.UtcNow}");
        Console.WriteLine("=== End Debug Info ===\n");

        // Ensure proper URL formatting
        string baseUrl = Regex.Replace(ConfigManager.ApiUrl ?? "", @"(https?://[^/]+)/?.*", "$1");
        string endpoint = "register-kill";
        string fullUrl = $"{baseUrl}/{endpoint}";

        var response = await httpClient.PostAsync(fullUrl, new StringContent(jsonData, Encoding.UTF8, "application/json"));
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
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response: {responseContent}");

            // Add the hash to our recorded hashes
            _recordedKillHashes.Add(hash);

            // Only process streamer data if streamlink is enabled
            if (ConfigManager.StreamlinkEnabled == 1)
            {
                ProcessStreamerResponse(responseContent);
            }
        }
    }

    public static void ProcessStreamerResponse(string responseContent)
    {
        try
        {
            var responseData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);
            if (responseData != null && responseData.TryGetValue("streamer", out JsonElement streamerElement))
            {
                string streamerHandle = streamerElement.GetString() ?? string.Empty;
                if (!string.IsNullOrEmpty(streamerHandle))
                {
                    // Sanitize the streamer handle before using it, this is to prevent any malicious instructions. 
                    string sanitizedHandle = SanitizeStreamerHandle(streamerHandle);
                    if (!string.IsNullOrEmpty(sanitizedHandle))
                    {
                        TrackREventDispatcher.OnStreamlinkRecordEvent(sanitizedHandle);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing streamer from response: {ex.Message}");
        }
    }

    private static string SanitizeStreamerHandle(string handle)
    {
        // Twitch usernames 4-25 characters, letters, numbers and underscores.
        if (Regex.IsMatch(handle, @"^[a-zA-Z0-9_]{4,25}$"))
        {
            return handle.ToLower(); // Api won't return anything other than lowercase but just in case. 
        }

        return string.Empty; // Reject invalid handles
    }
}