using System.ComponentModel;

namespace Aiml; 
public class GossipEventArgs(string message) : HandledEventArgs {
	public string Message { get; set; } = message;
}

public class LogMessageEventArgs(LogLevel level, string message) : HandledEventArgs {
	public LogLevel Level { get; } = level;
	public string Message { get; set; } = message;
}
