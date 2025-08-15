using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using AutoTrackR2.LogEventHandlers;

namespace AutoTrackR2;


// Represents a single log entry
// This is the object that will be passed to each handler, mostly for convenience
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public required string? Message { get; set; }
}

enum GameProcessState
{
    NotRunning,
    Running,
    Unknown
}

public class LogHandler
{
    private string _logPath;
    private FileStream? _fileStream;
    private StreamReader? _reader;
    private Thread? _monitorThread;
    private CancellationTokenSource? _cancellationTokenSource;
    private GameProcessState _gameProcessState = GameProcessState.NotRunning;
    private bool _isMonitoring = false;
    private bool _isInitializing = false;
    private System.Timers.Timer? _initializationTimer;
    private bool _isUsingPlaceholderFile = false;

    // Static property to track if TrackR is fully initialized and ready for real-time features
    public static bool IsTrackRReady { get; private set; } = false;

    public bool IsMonitoring => _isMonitoring;
    public bool IsInitializing => _isInitializing;
    public bool IsUsingPlaceholderFile => _isUsingPlaceholderFile;
    public bool IsProperlyConfigured => !_isUsingPlaceholderFile && !string.IsNullOrEmpty(_logPath);

    // Handlers that should be run on every log entry
    // Overlap with _startupEventHandlers is fine
    private readonly List<ILogEventHandler> _eventHandlers = [
        new LoginEvent(),
        new InstancedInteriorEvent(),
        new InArenaCommanderEvent(),
        new InPersistentUniverseEvent(),
        new GameVersionEvent(),
        new JumpDriveStateChangedEvent(),
        new RequestJumpFailedEvent(),
        new VehicleDestructionEvent(),
        new ActorDeathEvent(),
        new VehicleControlEvent(),
        new VehicleControlClearEvent()
    ];

    public LogHandler(string? logPath)
    {
        if (string.IsNullOrEmpty(logPath))
        {
            // Set a default path instead of throwing an exception
            _logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoTrackR2",
                "default.log"
            );
            _isUsingPlaceholderFile = true;
        }
        else
        {
            _logPath = logPath;
            _isUsingPlaceholderFile = false;
        }
    }

    public void Initialize()
    {
        // Ensure the directory exists
        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // If the log file doesn't exist, create a placeholder file
        if (!File.Exists(_logPath))
        {
            try
            {
                File.WriteAllText(_logPath, "# AutoTrackR2 placeholder log file\n# Please configure the correct Star Citizen log file path in settings\n");
                _isUsingPlaceholderFile = true;
            }
            catch (Exception ex)
            {
                // If we can't create the file, just return without initializing
                Console.WriteLine($"Could not create placeholder log file: {ex.Message}");
                return;
            }
        }

        // Check if Star Citizen is running
        if (!IsStarCitizenRunning())
        {
            StartInitializationDelay();
            return;
        }

        InitializeLogHandler();
    }

    public void InitializeForImport()
    {
        // Force initialize for import purposes without requiring Star Citizen to be running
        Console.WriteLine("Initializing LogHandler for import process");

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // If the log file doesn't exist, create a placeholder file
        if (!File.Exists(_logPath))
        {
            try
            {
                File.WriteAllText(_logPath, "# AutoTrackR2 placeholder log file\n# Please configure the correct Star Citizen log file path in settings\n");
                _isUsingPlaceholderFile = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not create placeholder log file: {ex.Message}");
                return;
            }
        }

        // Force initialize without Star Citizen check
        InitializeLogHandler();
    }

    private void StartInitializationDelay()
    {
        _isInitializing = true;
        _initializationTimer = new System.Timers.Timer(20000); // 20 seconds
        _initializationTimer.Elapsed += (sender, e) =>
        {
            _isInitializing = false;
            _initializationTimer?.Stop();
            _initializationTimer?.Dispose();
            _initializationTimer = null;

            if (IsStarCitizenRunning())
            {
                InitializeLogHandler();
            }
        };
        _initializationTimer.Start();
    }

    private void InitializeLogHandler()
    {
        try
        {
            _fileStream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _reader = new StreamReader(_fileStream);

            while (_reader.ReadLine() is { } line)
            {
                HandleLogEntry(line);
            }

            StartMonitoring();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize log handler: {ex.Message}");
            // Don't start monitoring if we can't access the file
            _isUsingPlaceholderFile = true;
        }
    }

    private bool IsStarCitizenRunning()
    {
        return Process.GetProcessesByName("StarCitizen").Length > 0;
    }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        _cancellationTokenSource = new CancellationTokenSource();
        _monitorThread = new Thread(() => MonitorLog(_cancellationTokenSource.Token));
        _monitorThread.Start();
        _isMonitoring = true;

        // TrackR is now ready to process real-time features
        IsTrackRReady = true;
        Console.WriteLine("🎯 TrackR is now ready - real-time features enabled");
        Console.WriteLine("✅ Startup protection lifted - visor wipe, video record, kill streaks, and streamlink are now active");
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        _cancellationTokenSource?.Cancel();
        _monitorThread?.Join();
        _reader?.Close();
        _fileStream?.Close();
        _isMonitoring = false;
    }

    // Parse a single line of the log file and run matching handlers
    private void HandleLogEntry(string line)
    {
        // Console.WriteLine(line);
        foreach (var handler in _eventHandlers)
        {
            var match = handler.Pattern.Match(line);
            if (!match.Success) continue;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = line
            };
            handler.Handle(entry);
            break;
        }
    }

    private void MonitorLog(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_reader == null || _fileStream == null)
            {
                break;
            }

            CheckGameProcessState();

            List<string> lines = new List<string>();
            while (_reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }

            foreach (var line in lines)
            {
                // start new thread to handle log entry
                var thread = new Thread(() => HandleLogEntry(line));
                thread.Start();
                // Console.WriteLine(line);
            }

            {
                // Wait for new lines to be written to the log file
                Thread.Sleep(500);
            }
        }
        Console.WriteLine("Monitor thread stopped");
    }

    private void CheckGameProcessState()
    {
        // Check if the game process is running by window name
        var process = Process.GetProcesses().FirstOrDefault(p => p.MainWindowTitle == "Star Citizen");

        GameProcessState newGameProcessState = process != null ? GameProcessState.Running : GameProcessState.NotRunning;
        if (newGameProcessState == GameProcessState.Running && _gameProcessState == GameProcessState.NotRunning)
        {
            // Game process went from NotRunning to Running, wait 20 seconds before reloading
            Console.WriteLine("Game process started, waiting 20 seconds before initializing...");
            _isInitializing = true;

            _initializationTimer = new System.Timers.Timer(20000); // 20 seconds
            _initializationTimer.Elapsed += (sender, e) =>
            {
                _isInitializing = false;
                _initializationTimer?.Stop();
                _initializationTimer?.Dispose();
                _initializationTimer = null;

                Console.WriteLine("Initialization delay complete, reloading log file");
                _reader?.Close();
                _fileStream?.Close();

                _fileStream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _reader = new StreamReader(_fileStream);
            };
            _initializationTimer.Start();
        }
        _gameProcessState = newGameProcessState;
    }

    public List<ILogEventHandler> GetEventHandlers()
    {
        return new List<ILogEventHandler>(_eventHandlers);
    }
}