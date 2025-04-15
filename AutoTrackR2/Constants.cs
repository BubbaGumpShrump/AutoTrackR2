namespace AutoTrackR2.Constants;

public static class AppConstants
{
  public static string Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? "2.0";
}