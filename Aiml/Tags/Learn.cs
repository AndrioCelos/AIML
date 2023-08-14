using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Processes the content as AIML and adds it to the bot's brain, temporarily and for the current user only.</summary>
	/// <remarks>
	///		<para>Unlike other elements with content, the content of this element is not normally evaluated until the newly-added category is called.
	///			However, the special child element <c>eval</c> is replaced with the result of evaluating its own content.</para>
	///		<para>This element is defined by the AIML 2.0 specification.</para>
	/// </remarks>
	/// <seealso cref="AddTriple"/><seealso cref="LearnF"/><seealso cref="Set"/>
	public sealed class Learn(XmlNode node) : TemplateNode {
		public XmlNode Node { get; } = node;

		public override string Evaluate(RequestProcess process) {
			// Evaluate <eval> tags.
			var node = this.Node.Clone();
			this.ProcessXml(node, process);

			// Learn the result.
			process.Log(LogLevel.Diagnostic, $"In element <learn>: learning new category for {process.User.ID}: {node.OuterXml}");
			var loader = new AimlLoader(process.Bot);
			loader.ProcessCategory(process.User.Graphmaster, node, null);

			return string.Empty;
		}

		public static Learn FromXml(XmlNode node, AimlLoader loader) => new(node);

		private void ProcessXml(XmlNode node, RequestProcess process) {
			for (var i = 0; i < node.ChildNodes.Count; ++i) {
				var node2 = node.ChildNodes[i];
				if (node2.NodeType == XmlNodeType.Element) {
					if (node2.Name.Equals("eval", StringComparison.InvariantCultureIgnoreCase)) {
						var tags = TemplateElementCollection.FromXml(node2, process.Bot.AimlLoader);
						node2.ParentNode.ReplaceChild(node.OwnerDocument.CreateTextNode(tags.Evaluate(process)), node2);
					} else
						this.ProcessXml(node2, process);
				}
			}
		}
	}
}
