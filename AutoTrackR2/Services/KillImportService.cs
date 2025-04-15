using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using AutoTrackR2.Constants;

namespace AutoTrackR2.Services;

public class KillImportService
{
  public class KillData
  {
    public int id { get; set; }
    public string attacker_ship_name { get; set; } = string.Empty;
    public string victim_ship_name { get; set; } = string.Empty;
    public string nickname { get; set; } = string.Empty;
    public string? avatar { get; set; }
    public JsonElement rsi { get; set; }
    public string enlisted { get; set; } = string.Empty;
    public string? org_name { get; set; }
    public string? slug { get; set; }
    public string weapon { get; set; } = string.Empty;
    public string method { get; set; } = string.Empty;
    public string created_at { get; set; } = string.Empty;
    public string game_version { get; set; } = string.Empty;
    public string gamemode { get; set; } = string.Empty;
    public string trackr_version { get; set; } = string.Empty;
    public string hash { get; set; } = string.Empty;
    public string reported_time { get; set; } = string.Empty;
  }

  public class KillResponse
  {
    public List<KillData> kills { get; set; } = new();
    public int total { get; set; }
    public int page { get; set; }
    public int per_page { get; set; }
  }

  private static string FormatField(string? value)
  {
    if (string.IsNullOrEmpty(value))
    {
      return "\"\"";
    }
    // Why the hell did we need to use a .csv file? Fisk you owe me. 
    value = value.Replace(",", "");
    // Escape any existing double quotes by doubling them because Fisk.
    value = value.Replace("\"", "\"\"");
    // Always wrap in quotes to handle any special characters because Fisk.
    return $"\"{value}\"";
  }

  private static string FormatEnlistedDate(string isoDate)
  {
    if (DateTime.TryParse(isoDate, out DateTime date))
    {
      return $"\"{date:MMM dd yyyy}\"";
    }
    return "\"\"";
  }

  private static string GetUnixTimestamp(string isoTimestamp)
  {
    if (DateTime.TryParse(isoTimestamp, out DateTime dateTime))
    {
      return $"\"{((DateTimeOffset)dateTime).ToUnixTimeSeconds()}\"";
    }
    return "\"0\"";
  }

  private static string GetRsiValue(JsonElement rsiElement)
  {
    try
    {
      if (rsiElement.ValueKind == JsonValueKind.Number)
      {
        return rsiElement.GetInt64().ToString();
      }
      else if (rsiElement.ValueKind == JsonValueKind.String)
      {
        if (long.TryParse(rsiElement.GetString(), out long result))
        {
          return result.ToString();
        }
      }
    }
    catch
    {
      // If any error occurs, return -1
    }
    return "-1";
  }

  public async Task ImportKillsFromApi(string apiUrl, string apiKey)
  {
    if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
    {
      MessageBox.Show("Please configure API URL and API Key first.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }

    try
    {
      // Configure HttpClient with TLS 1.2
      var handler = new HttpClientHandler
      {
        SslProtocols = SslProtocols.Tls12
      };

      using (var client = new HttpClient(handler))
      {
        // Set headers
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AutoTrackR2");

        // Get base URL
        string baseUrl = Regex.Replace(apiUrl, @"(https?://[^/]+)/?.*", "$1");
        string endpoint = "player/kills";

        // Create CSV content
        var csvContent = new StringBuilder();
        // Add header
        csvContent.AppendLine("KillTime,EnemyPilot,EnemyShip,Enlisted,RecordNumber,OrgAffiliation,Player,Weapon,Ship,Method,Mode,GameVersion,TrackRver,Logged,PFP,Hash");

        int currentPage = 1;
        int totalKills = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
          // Create JSON body with version and page
          var jsonBody = new { version = AppConstants.Version, page = currentPage };
          var content = new StringContent(JsonSerializer.Serialize(jsonBody), Encoding.UTF8, "application/json");

          // Send POST request
          var response = await client.PostAsync($"{baseUrl}/{endpoint}", content);

          if (response.IsSuccessStatusCode)
          {
            var responseContent = await response.Content.ReadAsStringAsync();
            var killResponse = JsonSerializer.Deserialize<KillResponse>(responseContent);

            if (killResponse?.kills != null && killResponse.kills.Count > 0)
            {
              // Sort kills by timestamp in ascending order
              var sortedKills = killResponse.kills.OrderBy(k => k.created_at).ToList();

              foreach (var kill in sortedKills)
              {
                var fields = new[]
                {
                                    GetUnixTimestamp(kill.created_at),                // KillTime
                                    FormatField(kill.nickname),                       // EnemyPilot
                                    FormatField(kill.victim_ship_name),              // EnemyShip
                                    FormatEnlistedDate(kill.enlisted),               // Enlisted
                                    FormatField(GetRsiValue(kill.rsi)),              // RecordNumber
                                    FormatField(kill.org_name),                      // OrgAffiliation
                                    FormatField(kill.nickname),                      // Player
                                    FormatField(kill.weapon),                        // Weapon
                                    FormatField(kill.attacker_ship_name),           // Ship
                                    FormatField(kill.method),                       // Method
                                    FormatField(kill.gamemode),                     // Mode
                                    FormatField(kill.game_version),                // GameVersion
                                    FormatField(kill.trackr_version),              // TrackRver
                                    FormatField(""),                               // Logged
                                    FormatField(kill.avatar),                      // PFP
                                    FormatField(kill.hash)                         // Hash
                                };
                csvContent.AppendLine(string.Join(",", fields));
              }

              totalKills += killResponse.kills.Count;

              // Check if we have more pages
              hasMorePages = killResponse.kills.Count == killResponse.per_page;
              currentPage++;
            }
            else
            {
              hasMorePages = false;
            }
          }
          else
          {
            MessageBox.Show($"Error: {response.StatusCode} - {response.ReasonPhrase}", "API Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
          }
        }

        // Save the CSV to AppData
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoTrackR2",
            "kill-log.csv"
        );

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(appDataPath)!);

        // Write the CSV to file with UTF-8 encoding without BOM
        await File.WriteAllTextAsync(appDataPath, csvContent.ToString(), new UTF8Encoding(false));

        MessageBox.Show($"Successfully imported {totalKills} kills and saved to kill-log.csv\n\nPlease restart TrackR to apply the changes.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
      }
    }
    catch (Exception ex)
    {
      MessageBox.Show($"Failed to import kill data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}