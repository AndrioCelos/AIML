using System.Xml;

namespace Aiml; 
public partial class TemplateNode {
	/// <summary>Logs and executes an application-defined event handler with its content.</summary>
	/// <remarks>This element is defined by the AIML 1.1 specification and deprecated by the AIML 2.0 specification.</remarks>
	public sealed class Gossip(TemplateElementCollection children) : RecursiveTemplateTag(children) {
		public override string Evaluate(RequestProcess process) {
			var message = this.Children?.Evaluate(process) ?? "";
			process.Bot.WriteGossip(process, message);
			return message;
		}

		public static Gossip FromXml(XmlNode node, AimlLoader loader) => new(TemplateElementCollection.FromXml(node, loader));
	}
}
