using System.Xml;

namespace Aiml.Tags;
/// <summary>Processes the content as AIML and adds it to the bot's brain, temporarily and for the current user only.</summary>
/// <remarks>
///		<para>Unlike other elements with content, the content of this element is not normally evaluated until the newly-added category is called.
///			However, the special child element <c>eval</c> is replaced with the result of evaluating its own content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="LearnF"/><seealso cref="Set"/>
public sealed class Learn(XmlElement el) : TemplateNode {
	public XmlElement XmlElement { get; } = el;

	public override string Evaluate(RequestProcess process) {
		// Evaluate <eval> tags.
		var node = (XmlElement) this.XmlElement.Clone();
		ProcessXml(node, process);

		// Learn the result.
		process.Log(LogLevel.Diagnostic, $"In element <learn>: learning new category for {process.User.ID}: {node.OuterXml}");
		var loader = new AimlLoader(process.Bot);
		loader.ProcessCategory(process.User.Graphmaster, node, null);

		return string.Empty;
	}

	internal static void ProcessXml(XmlElement el, RequestProcess process) {
		for (var i = 0; i < el.ChildNodes.Count; ++i) {
			var childNode = el.ChildNodes[i];
			if (childNode is XmlElement childElement) {
				if (childNode.Name.Equals("eval", StringComparison.OrdinalIgnoreCase)) {
					var tags = TemplateElementCollection.FromXml(childElement, process.Bot.AimlLoader!);
					el.ReplaceChild(el.OwnerDocument.CreateTextNode(tags.Evaluate(process)), childElement);
				} else
					ProcessXml(childElement, process);
			}
		}
	}
}
