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

public class LogHandler(string logPath)
{
    private readonly string? _logPath = logPath;
    private FileStream? _fileStream;
    private StreamReader? _reader;
    private GameProcessState _gameProcessState = GameProcessState.Unknown;

    private CancellationTokenSource cancellationToken = new CancellationTokenSource(); 
    Thread? monitorThread;
    
    // Handlers that should be run on every log entry
    // Overlap with _startupEventHandlers is fine
    private readonly List<ILogEventHandler> _eventHandlers = [
        new LoginEvent(),
        new InstancedInteriorEvent(),
        new InArenaCommanderEvent(),
        new InPersistentUniverseEvent(),
        new GameVersionEvent(),
        new JumpDriveStateChangedEvent(),
        new RequestJumpFailedEvent()
    ];

    // Initialize the LogHandler and run all startup handlers
    public void Initialize()
    {
        if (!File.Exists(_logPath))
        {
            throw new FileNotFoundException("Log file not found", _logPath);
        }
        
        _fileStream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _reader = new StreamReader(_fileStream);
        
        while (_reader.ReadLine() is { } line)
        {
            HandleLogEntry(line);
        }

        // Ensures that any deaths already in log aren't sent to the APIs until the monitor thread is running
        _eventHandlers.Add(new ActorDeathEvent());
        
        monitorThread = new Thread(() => MonitorLog(cancellationToken.Token));
        monitorThread.Start();
    }

    public void Stop()
    {
        // Stop the monitor thread
        cancellationToken?.Cancel();
        _reader?.Close();
        _fileStream?.Close();
    }
    
    // Parse a single line of the log file and run matching handlers
    private void HandleLogEntry(string line)
    {
        Console.WriteLine(line);
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
                Thread.Sleep(1000);
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
            // Game process went from NotRunning to Running, so reload the Game.log file
            Console.WriteLine("Game process started, reloading log file");
                
            _reader?.Close();
            _fileStream?.Close();
            
            _fileStream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _reader = new StreamReader(_fileStream);
        }
        
        _gameProcessState = newGameProcessState;
    }
}