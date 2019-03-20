using System.ComponentModel;

namespace Aiml {
	public class GossipEventArgs : HandledEventArgs {
		public string Message { get; set; }

		public GossipEventArgs(string message) {
			this.Message = message;
		}
	}

	public class LogMessageEventArgs : HandledEventArgs {
		public LogLevel Level { get; }
		public string Message { get; set; }

		public LogMessageEventArgs(LogLevel level, string message) {
			this.Level = level;
			this.Message = message;
		}
	}
}
