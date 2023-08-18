using System.ComponentModel;

namespace Aiml;
public class GossipEventArgs(string message) : HandledEventArgs {
	public string Message { get; set; } = message;
}

public class LogMessageEventArgs(LogLevel level, string message) : HandledEventArgs {
	public LogLevel Level { get; } = level;
	public string Message { get; set; } = message;
}

public class PostbackRequestEventArgs(Request request) : EventArgs {
	public Request Request { get; } = request;
}

public class PostbackResponseEventArgs(Response response) : EventArgs {
	public Response Response { get; } = response;
}
