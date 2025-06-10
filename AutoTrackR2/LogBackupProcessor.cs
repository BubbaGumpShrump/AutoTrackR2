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

  public LogBackupProcessor(string logBackupsPath, KillHistoryManager killHistoryManager, List<ILogEventHandler> logEventHandlers)
  {
    _logBackupsPath = logBackupsPath;
    _killHistoryManager = killHistoryManager;
    _logEventHandlers = logEventHandlers;
  }

  public async Task ProcessLogBackupsAsync(Action<string>? onLogFileProcessed = null)
  {
    if (!Directory.Exists(_logBackupsPath))
    {
      Console.WriteLine($"Log backups directory not found: {_logBackupsPath}");
      return;
    }

    var logFiles = Directory.GetFiles(_logBackupsPath, "*.log", SearchOption.AllDirectories)
      .Where(file => File.GetLastWriteTime(file) >= new DateTime(2025, 3, 27))
      .ToArray();
    Array.Sort(logFiles); // Process files in chronological order

    for (int i = 0; i < logFiles.Length; i++)
    {
      var logFile = logFiles[i];
      onLogFileProcessed?.Invoke(logFile);
      await ProcessLogFileAsync(logFile);
    }
  }

  private async Task ProcessLogFileAsync(string logFilePath)
  {
    try
    {
      using var reader = new StreamReader(logFilePath);
      string? line;
      var lines = new List<string>();
      while ((line = await reader.ReadLineAsync()) != null)
      {
        lines.Add(line);
      }
      int actorDeathCount = 0;
      await Task.Run(() => Parallel.ForEach(lines, line =>
      {
        var entry = new LogEntry { Message = line };
        foreach (var handler in _logEventHandlers)
        {
          if (handler.Pattern.IsMatch(line))
          {
            handler.Handle(entry);
            if (handler is ActorDeathEvent)
            {
              Interlocked.Increment(ref actorDeathCount);
            }
          }
        }
      }));
      Console.WriteLine($"Processed {actorDeathCount} actor deaths in {logFilePath}");
      // Wait 5 seconds after processing the file before moving to the next file
      await Task.Delay(TimeSpan.FromSeconds(5));
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error processing log file {logFilePath}: {ex.Message}");
    }
  }
}