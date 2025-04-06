using System.Text.RegularExpressions;

namespace AutoTrackR2.LogEventHandlers;

public interface ILogEventHandler
{ 
    Regex Pattern { get; }
    void Handle(LogEntry entry);

}