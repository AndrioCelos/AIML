using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace Aiml;
[Serializable]
public class AimlException : XmlException {
	public AimlException(string message, XElement element) : base(AugmentMessage(message, element)) { }
	public AimlException(string message, XElement element, Exception innerException) : base(AugmentMessage(message, element), innerException) { }
	protected AimlException(SerializationInfo info, StreamingContext context) : base(info, context) { }

	private static string AugmentMessage(string message, XElement element)
		=> ((IXmlLineInfo) element).HasLineInfo()
			? $"In element <{element.Name}>: {message}, {(element.BaseUri != "" ? element.BaseUri : "<no URI>")} line {((IXmlLineInfo) element).LineNumber} column {((IXmlLineInfo) element).LinePosition}"
			: element.BaseUri != ""
			? $"In element <{element.Name}>: {message}, {element.BaseUri}"
			: $"In element <{element.Name}>: {message}";
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
