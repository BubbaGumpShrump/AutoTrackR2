using System.Diagnostics;
using System.IO;

namespace AutoTrackR2;

public class StreamlinkHandler
{
  public StreamlinkHandler()
  {
    TrackREventDispatcher.StreamlinkRecordEvent += HandleStreamlinkRecord;
  }

  public static bool IsStreamlinkInstalled()
  {
    try
    {
      var checkInfo = new ProcessStartInfo
      {
        FileName = "where",
        Arguments = "streamlink",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
      };

      using (var process = Process.Start(checkInfo))
      {
        if (process == null)
        {
          return false;
        }

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode == 0 && !string.IsNullOrEmpty(output);
      }
    }
    catch
    {
      return false;
    }
  }

  private async void HandleStreamlinkRecord(string streamerHandle)
  {
    if (ConfigManager.StreamlinkEnabled != 1)
    {
      return;
    }

    try
    {
      var outputPath = Path.Combine(
          ConfigManager.VideoPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
          $"{streamerHandle}_{DateTime.Now:yyyyMMdd_HHmmss}.mp4"
      );

      // Calculate the duration for recording (30 seconds before + configured duration after)
      var totalDuration = 30 + ConfigManager.StreamlinkDuration;

      var recordInfo = new ProcessStartInfo
      {
        FileName = "streamlink",
        Arguments = $"https://www.twitch.tv/{streamerHandle} best --twitch-disable-ads --hls-live-edge 30 --hls-duration {totalDuration} -o {outputPath}",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
      };

      using (var process = Process.Start(recordInfo))
      {
        if (process == null)
        {
          return;
        }

        // Wait for the recording to complete
        await process.WaitForExitAsync();
      }
    }
    catch (Exception ex)
    {
      // Log the error but don't crash the application
      Debug.WriteLine($"Error recording streamlink clip: {ex.Message}");
    }
  }
}