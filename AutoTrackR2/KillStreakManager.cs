using System.Media;
using System.Timers;
using System.IO;

namespace AutoTrackR2;

public class KillStreakManager : IDisposable
{
  private readonly Queue<string> _soundQueue = new();
  private readonly System.Timers.Timer _killStreakTimer = new(5000); // 5 seconds between kills for streak
  private int _currentKills = 0;
  private int _totalKills = 0;
  private readonly string _soundsPath;
  private readonly object _lock = new();
  private SoundPlayer? _currentPlayer;
  private bool _isPlaying = false;
  private bool _disposed = false;
  private Task? _currentPlaybackTask;

  public KillStreakManager(string soundsPath)
  {
    _soundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds");
    _killStreakTimer.Elapsed += OnKillStreakTimerElapsed;
    Console.WriteLine($"KillStreakManager initialized with sounds path: {_soundsPath}");
  }

  public void OnKill()
  {
    lock (_lock)
    {
      if (_disposed) return;

      _currentKills++;
      _totalKills++;
      _killStreakTimer.Stop();
      _killStreakTimer.Start();

      // Handle multi-kill announcements
      string? multiKillSound = _currentKills switch
      {
        2 => "double_kill.wav",
        3 => "triple_kill.wav",
        4 => "overkill.wav",
        5 => "killtacular.wav",
        6 => "killtrocity.wav",
        7 => "killimanjaro.wav",
        8 => "killtastrophe.wav",
        9 => "killpocalypse.wav",
        10 => "killionaire.wav",
        _ => null
      };

      // Handle spree announcements
      string? spreeSound = _totalKills switch
      {
        5 => "killing_spree.wav",
        10 => "killing_frenzy.wav",
        15 => "running_riot.wav",
        20 => "rampage.wav",
        25 => "untouchable.wav",
        30 => "invincible.wav",
        35 => "unstoppable.wav",
        40 => "hells_janitor.wav",
        45 => "perfection.wav",
        _ => null
      };

      // Queue up the sounds if they exist
      if (multiKillSound != null)
      {
        string soundPath = Path.Combine(_soundsPath, multiKillSound);
        Console.WriteLine($"Queueing multi-kill sound: {soundPath}");
        _soundQueue.Enqueue(soundPath);
      }
      if (spreeSound != null)
      {
        string soundPath = Path.Combine(_soundsPath, spreeSound);
        Console.WriteLine($"Queueing spree sound: {soundPath}");
        _soundQueue.Enqueue(soundPath);
      }

      // Only start playing if not already playing
      if (!_isPlaying && _soundQueue.Count > 0)
      {
        _currentPlaybackTask = Task.Run(() => PlayNextSound());
      }
    }
  }

  public void OnDeath()
  {
    lock (_lock)
    {
      if (_disposed) return;

      _totalKills = 0;
      _currentKills = 0;
      _killStreakTimer.Stop();
      Console.WriteLine("Kill streak reset due to death");
    }
  }

  private void OnKillStreakTimerElapsed(object? sender, ElapsedEventArgs e)
  {
    lock (_lock)
    {
      if (_disposed) return;

      _currentKills = 0;
      _killStreakTimer.Stop();
      Console.WriteLine("Kill streak reset due to timeout");
    }
  }

  private void PlayNextSound()
  {
    if (_soundQueue.Count == 0 || _disposed) return;

    string soundPath;
    lock (_lock)
    {
      if (_soundQueue.Count == 0 || _disposed) return;
      soundPath = _soundQueue.Dequeue();
      _isPlaying = true;
    }

    Console.WriteLine($"Attempting to play sound: {soundPath}");

    try
    {
      if (!File.Exists(soundPath))
      {
        Console.WriteLine($"Sound file not found: {soundPath}");
        lock (_lock)
        {
          _isPlaying = false;
          if (_soundQueue.Count > 0)
          {
            _currentPlaybackTask = Task.Run(() => PlayNextSound());
          }
        }
        return;
      }

      // Stop any currently playing sound
      lock (_lock)
      {
        _currentPlayer?.Stop();
        _currentPlayer?.Dispose();
        _currentPlayer = null;
      }

      // Create a new SoundPlayer
      var player = new SoundPlayer(soundPath);
      player.LoadCompleted += (sender, e) =>
      {
        if (e.Error != null)
        {
          Console.WriteLine($"Error loading sound {soundPath}: {e.Error.Message}");
          lock (_lock)
          {
            _isPlaying = false;
            if (_soundQueue.Count > 0)
            {
              _currentPlaybackTask = Task.Run(() => PlayNextSound());
            }
          }
        }
      };

      player.SoundLocationChanged += (sender, e) =>
      {
        Console.WriteLine($"Sound location changed: {soundPath}");
      };

      player.PlaySync();

      lock (_lock)
      {
        if (_disposed)
        {
          player.Dispose();
          return;
        }
        _currentPlayer = player;
        _isPlaying = false;
        if (_soundQueue.Count > 0)
        {
          _currentPlaybackTask = Task.Run(() => PlayNextSound());
        }
      }

      Console.WriteLine($"Successfully played sound: {soundPath}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error playing sound {soundPath}: {ex.Message}");
      lock (_lock)
      {
        _isPlaying = false;
        if (_soundQueue.Count > 0)
        {
          _currentPlaybackTask = Task.Run(() => PlayNextSound());
        }
      }
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_disposed) return;

    if (disposing)
    {
      lock (_lock)
      {
        _disposed = true;
        _killStreakTimer.Stop();
        _killStreakTimer.Dispose();
        _currentPlayer?.Stop();
        _currentPlayer?.Dispose();
        _currentPlayer = null;
        _currentPlaybackTask?.Wait();
      }
    }
  }
}