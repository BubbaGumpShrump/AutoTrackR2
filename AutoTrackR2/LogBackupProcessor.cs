using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AutoTrackR2.LogEventHandlers;
using System.Windows;
using System.Linq;
using System.Threading;

namespace AutoTrackR2;

public class LogBackupProcessor
{
  private readonly string _logBackupsPath;
  private readonly KillHistoryManager _killHistoryManager;
  private readonly List<ILogEventHandler> _logEventHandlers;
  private readonly Regex _combinedPattern;

  public LogBackupProcessor(string logBackupsPath, KillHistoryManager killHistoryManager, List<ILogEventHandler> logEventHandlers)
  {
    _logBackupsPath = logBackupsPath;
    _killHistoryManager = killHistoryManager;
    _logEventHandlers = logEventHandlers;

    Console.WriteLine($"LogBackupProcessor initialized with {logEventHandlers.Count} event handlers");

    // Create a combined regex pattern for all handlers to quickly filter lines
    var patterns = logEventHandlers.Select(h => h.Pattern.ToString()).ToArray();
    _combinedPattern = new Regex(string.Join("|", patterns), RegexOptions.Compiled | RegexOptions.IgnoreCase);

    Console.WriteLine($"Combined pattern created: {_combinedPattern}");

    // Verify we have ActorDeathEvent handlers
    var actorDeathHandlers = logEventHandlers.OfType<ActorDeathEvent>().ToList();
    Console.WriteLine($"ActorDeathEvent handlers found: {actorDeathHandlers.Count}");
    foreach (var handler in actorDeathHandlers)
    {
      Console.WriteLine($"  ActorDeathEvent pattern: {handler.Pattern}");
    }
  }

  public async Task<(int TotalKillsFound, int TotalKillsImported, int TotalKillsNotImported, double ImportSuccessRate)> ProcessLogBackupsAsync(Action<string>? onLogFileProcessed = null)
  {
    if (!Directory.Exists(_logBackupsPath))
    {
      Console.WriteLine($"Log backups directory not found: {_logBackupsPath}");
      return (0, 0, 0, 0.0);
    }

    // Create a context to suppress real-time features during import
    using var importContext = new ImportContext();
    Console.WriteLine("Real-time features temporarily disabled for log backup import (visor wipe, video record, kill streaks, streamlink)");

    var logFiles = Directory.GetFiles(_logBackupsPath, "*.log", SearchOption.AllDirectories)
      .Where(file => File.GetLastWriteTime(file) >= new DateTime(2025, 3, 27))
      .ToArray();
    Array.Sort(logFiles); // Process files in chronological order

    Console.WriteLine($"Found {logFiles.Length} log files to process");
    Console.WriteLine($"Event handlers available: {_logEventHandlers.Count}");
    foreach (var handler in _logEventHandlers)
    {
      Console.WriteLine($"  - {handler.GetType().Name}: {handler.Pattern}");
    }

    // Process files sequentially to ensure proper completion
    var totalKillsFound = 0;
    var totalKillsImported = 0;

    for (int i = 0; i < logFiles.Length; i++)
    {
      var logFile = logFiles[i];
      onLogFileProcessed?.Invoke(logFile);

      var fileResult = await ProcessLogFileAsync(logFile, i);
      totalKillsFound += fileResult.KillsFound;
      totalKillsImported += fileResult.KillsImported;
    }

    var totalKillsNotImported = totalKillsFound - totalKillsImported;
    var importSuccessRate = totalKillsFound > 0 ? (totalKillsImported * 100.0 / totalKillsFound) : 0.0;

    Console.WriteLine($"=== IMPORT SUMMARY ===");
    Console.WriteLine($"Total kills found: {totalKillsFound}");
    Console.WriteLine($"Total kills imported: {totalKillsImported}");
    Console.WriteLine($"Kills not imported: {totalKillsNotImported}");
    Console.WriteLine($"Import success rate: {importSuccessRate:F1}%");

    Console.WriteLine("Real-time features restored after log backup import");
    return (totalKillsFound, totalKillsImported, totalKillsNotImported, importSuccessRate);
  }

  private async Task<(int KillsFound, int KillsImported)> ProcessLogFileAsync(string logFilePath, int fileIndex)
  {
    try
    {
      Console.WriteLine($"Processing file {fileIndex + 1}: {Path.GetFileName(logFilePath)}");

      var killsFound = 0;
      var killsImported = 0;
      var relevantLines = new List<string>();

      // First pass: Quick filter using combined pattern to find relevant lines only
      using (var reader = new StreamReader(logFilePath))
      {
        string? line;
        var lineCount = 0;
        while ((line = await reader.ReadLineAsync()) != null)
        {
          lineCount++;
          // Only process lines that match any of our patterns
          if (_combinedPattern.IsMatch(line))
          {
            relevantLines.Add(line);
            if (relevantLines.Count <= 3) // Show first 3 matches for debugging
            {
              Console.WriteLine($"Line {lineCount} matched combined pattern: {line.Substring(0, Math.Min(100, line.Length))}...");
            }
          }
        }
        Console.WriteLine($"Total lines read: {lineCount}");
      }

      Console.WriteLine($"Found {relevantLines.Count} relevant lines out of total lines in {Path.GetFileName(logFilePath)}");

      if (relevantLines.Count == 0)
      {
        return (0, 0);
      }

      // Show first few relevant lines for debugging
      Console.WriteLine("First few relevant lines found:");
      for (int i = 0; i < Math.Min(3, relevantLines.Count); i++)
      {
        Console.WriteLine($"  Line {i + 1}: {relevantLines[i].Substring(0, Math.Min(100, relevantLines[i].Length))}...");
      }

      // Second pass: Process only the relevant lines
      var batches = relevantLines
        .Select((line, index) => new { Line = line, Index = index })
        .GroupBy(x => x.Index / 1000) // Process in batches of 1000
        .Select(g => g.Select(x => x.Line).ToList())
        .ToList();

      foreach (var batch in batches)
      {
        var batchResult = await ProcessBatch(batch);
        killsFound += batchResult.KillsFound;
        killsImported += batchResult.KillsImported;
      }

      Console.WriteLine($"File {Path.GetFileName(logFilePath)}: {killsFound} kills found, {killsImported} kills imported");

      return (killsFound, killsImported);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error processing log file {logFilePath}: {ex.Message}");
      return (0, 0);
    }
  }

  private async Task<(int KillsFound, int KillsImported)> ProcessBatch(List<string> lines)
  {
    var killsFound = 0;
    var killsImported = 0;

    Console.WriteLine($"Processing batch of {lines.Count} lines...");

    foreach (var line in lines)
    {
      var entry = new LogEntry { Message = line };

      // Find the matching handler and process
      foreach (var handler in _logEventHandlers)
      {
        if (handler.Pattern.IsMatch(line))
        {
          Console.WriteLine($"Line matched handler: {handler.GetType().Name}");

          // Count kills found
          if (handler is ActorDeathEvent)
          {
            killsFound++;
            Console.WriteLine($"Found kill #{killsFound}: {line.Substring(0, Math.Min(100, line.Length))}...");
          }

          try
          {
            // Attempt to import the kill - ensure we wait for completion
            await Task.Run(() => handler.Handle(entry));

            // Give a moment for the handler to complete its internal processing
            await Task.Delay(50);

            // If successful, count as imported
            if (handler is ActorDeathEvent)
            {
              killsImported++;
              Console.WriteLine($"Successfully imported kill #{killsImported}");
            }
          }
          catch (Exception ex)
          {
            Console.WriteLine($"Failed to import kill from line: {line.Substring(0, Math.Min(100, line.Length))}... Error: {ex.Message}");
          }

          break; // Found a match, no need to check other handlers
        }
      }
    }

    Console.WriteLine($"Batch complete: {killsFound} kills found, {killsImported} kills imported");
    return (killsFound, killsImported);
  }
}

// Context class to suppress real-time features during import
public class ImportContext : IDisposable
{
  private readonly int _originalVisorWipe;
  private readonly int _originalVideoRecord;
  private readonly int _originalKillStreakEnabled;
  private readonly int _originalStreamlinkEnabled;

  public ImportContext()
  {
    // Store original settings
    _originalVisorWipe = ConfigManager.VisorWipe;
    _originalVideoRecord = ConfigManager.VideoRecord;
    _originalKillStreakEnabled = ConfigManager.KillStreakEnabled;
    _originalStreamlinkEnabled = ConfigManager.StreamlinkEnabled;

    // Disable features during import
    ConfigManager.VisorWipe = 0;
    ConfigManager.VideoRecord = 0;
    ConfigManager.KillStreakEnabled = 0;
    ConfigManager.StreamlinkEnabled = 0;
  }

  public void Dispose()
  {
    // Restore original settings
    ConfigManager.VisorWipe = _originalVisorWipe;
    ConfigManager.VideoRecord = _originalVideoRecord;
    ConfigManager.KillStreakEnabled = _originalKillStreakEnabled;
    ConfigManager.StreamlinkEnabled = _originalStreamlinkEnabled;
  }
}