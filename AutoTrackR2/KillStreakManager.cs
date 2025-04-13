using System.Media;
using System.Timers;
using System.IO;
using NAudio.Wave;

namespace AutoTrackR2;

public class KillStreakManager : IDisposable
{
  private readonly Queue<string> _soundQueue = new();
  private readonly System.Timers.Timer _killStreakTimer = new(5000); // 5 seconds between kills for streak
  private int _currentKills = 0;
  private int _totalKills = 0;
  private readonly string _soundsPath;
  private readonly object _lock = new();
  private WaveOutEvent? _waveOut;
  private bool _isPlaying = false;
  private bool _disposed = false;

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
        2 => "double_kill.mp3",
        3 => "triple_kill.mp3",
        4 => "overkill.mp3",
        5 => "killtacular.mp3",
        6 => "killtrocity.mp3",
        7 => "killimanjaro.mp3",
        8 => "killtastrophe.mp3",
        9 => "killpocalypse.mp3",
        10 => "killionaire.mp3",
        _ => null
      };

      // Handle spree announcements
      string? spreeSound = _totalKills switch
      {
        5 => "killing_spree.mp3",
        10 => "killing_frenzy.mp3",
        15 => "running_riot.mp3",
        20 => "rampage.mp3",
        25 => "untouchable.mp3",
        30 => "invincible.mp3",
        35 => "unstoppable.mp3",
        40 => "hells_janitor.mp3",
        45 => "perfection.mp3",
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
        PlayNextSound();
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

    string soundPath = _soundQueue.Dequeue();
    Console.WriteLine($"Attempting to play sound: {soundPath}");

    try
    {
      if (!File.Exists(soundPath))
      {
        Console.WriteLine($"Sound file not found: {soundPath}");
        _isPlaying = false;
        if (_soundQueue.Count > 0)
        {
          PlayNextSound();
        }
        return;
      }

      // Stop any currently playing sound
      _waveOut?.Stop();
      _waveOut?.Dispose();
      _waveOut = null;

      // Create a new WaveOutEvent
      _waveOut = new WaveOutEvent();

      // Create a new AudioFileReader for the MP3 file
      using var audioFile = new AudioFileReader(soundPath);
      _waveOut.Init(audioFile);

      // Set up event handler for when playback finishes
      _waveOut.PlaybackStopped += (sender, e) =>
      {
        lock (_lock)
        {
          if (_disposed) return;

          _isPlaying = false;
          if (_soundQueue.Count > 0)
          {
            PlayNextSound();
          }
        }
      };

      _isPlaying = true;
      _waveOut.Play();

      Console.WriteLine($"Successfully played sound: {soundPath}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error playing sound {soundPath}: {ex.Message}");
      _isPlaying = false;
      if (_soundQueue.Count > 0)
      {
        PlayNextSound();
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
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
      }
    }
  }
}