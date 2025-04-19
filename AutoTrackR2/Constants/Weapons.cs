using System.Collections.Generic;

namespace AutoTrackR2.Constants;

public static class Weapons
{
  public static readonly List<string> List = new List<string>
    {
        "*energy*",
        "*ballistic*",
        "*toy*",
        "*multitool*",
        "*melee*",
        "*repair*",
        "*cutter*",
        "*tractor*",
        "*carryable*"
    };

  public static bool IsKnownWeapon(string weaponName)
  {
    if (List.Contains(weaponName))
      return true;

    return List.Any(pattern =>
        pattern.Contains("*") &&
        weaponName.Contains(pattern.Trim('*')));
  }
}