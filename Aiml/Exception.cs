using System;
using System.Runtime.Serialization;
using System.Xml;

namespace Aiml {
	[Serializable]
	public class AimlException : XmlException {
		public AimlException() : this("An error occurred while loading AIML.") { }
		public AimlException(string message) : base(message) { }
		public AimlException(string message, Exception inner) : base(message, inner) { }
		protected AimlException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class RecursionLimitException : Exception {
		public RecursionLimitException() : this("The request exceeded the AIML recursion limit.") { }
		public RecursionLimitException(string message) : base(message) { }
		public RecursionLimitException(string message, Exception inner) : base(message, inner) { }
		protected RecursionLimitException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class LoopLimitException : Exception {
		public LoopLimitException() : this("The request exceeded the AIML loop limit.") { }
		public LoopLimitException(string message) : base(message) { }
		public LoopLimitException(string message, Exception inner) : base(message, inner) { }
		protected LoopLimitException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
