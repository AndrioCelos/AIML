using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Recurses the text matched by the first message wildcard into a new request and returns the result.
		/// </summary>
		/// <remarks>
		///     This element is shorthand for <code><srai><star/></srai></code>.
		///     This element has no content.
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		public sealed class SR : TemplateNode {
			public override string Evaluate(RequestProcess process) {
				string text = process.star[0];
				process.Log(LogLevel.Diagnostic, "In element <sr>: processing text '" + text + "'.");
				var newRequest = new Aiml.Request(text, process.User, process.Bot);
				text = process.Bot.ProcessRequest(newRequest, false, false, process.RecursionDepth + 1, out _).ToString();
				process.Log(LogLevel.Diagnostic, "In element <sr>: the request returned '" + text + "'.");
				return text;
			}

			public static SR FromXml(XmlNode node, AimlLoader loader) {
				return new SR();  // The sr tag supports no properties.
			}
		}
	}
}
