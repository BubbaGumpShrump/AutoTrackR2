using System;

namespace AutoTrackR2;

/// <summary>
/// Manages real-time feature availability based on TrackR initialization state.
/// Prevents spam during startup by blocking features until TrackR is fully ready.
/// </summary>
public static class RealTimeFeatureManager
{
  /// <summary>
  /// Checks if a real-time feature should be enabled based on current TrackR state.
  /// </summary>
  /// <param name="featureName">Name of the feature for logging purposes</param>
  /// <returns>True if the feature should be enabled, false if it should be blocked</returns>
  public static bool ShouldEnableFeature(string featureName)
  {
    if (!LogHandler.IsTrackRReady)
    {
      Console.WriteLine($"[STARTUP PROTECTION] Blocking {featureName} - TrackR not yet initialized");
      return false;
    }

    return true;
  }

  /// <summary>
  /// Checks if visor wipe should be enabled.
  /// </summary>
  public static bool ShouldEnableVisorWipe()
  {
    return ShouldEnableFeature("Visor Wipe");
  }

  /// <summary>
  /// Checks if video recording should be enabled.
  /// </summary>
  public static bool ShouldEnableVideoRecord()
  {
    return ShouldEnableFeature("Video Record");
  }

  /// <summary>
  /// Checks if kill streak features should be enabled.
  /// </summary>
  public static bool ShouldEnableKillStreak()
  {
    return ShouldEnableFeature("Kill Streak");
  }

  /// <summary>
  /// Checks if streamlink features should be enabled.
  /// </summary>
  public static bool ShouldEnableStreamlink()
  {
    return ShouldEnableFeature("Streamlink");
  }

  /// <summary>
  /// Gets the current TrackR readiness status.
  /// </summary>
  public static bool IsTrackRReady => LogHandler.IsTrackRReady;
}
