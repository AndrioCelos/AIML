using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Returns the entire text of a previous input to the bot, consisting of one or more sentences.</summary>
	/// <remarks>
	///		<para>This element has the following attribute:</para>
	///		<list type="table">
	///			<item>
	///				<term><c>index</c></term>
	///				<description>a number specifying which line to return. 1 returns the previous request, and so on.
	///					If omitted, 1 is used.</description>
	///			</item>
	///		</list>
	///		<para>This element has no content.</para>
	///		<para>This element is defined by the AIML 2.0 specification.</para>
	/// </remarks>
	/// <seealso cref="Input"/><seealso cref="Response"/><seealso cref="That"/>
	public sealed class Request(TemplateElementCollection index) : TemplateNode {
		public TemplateElementCollection Index { get; set; } = index;

		public override string Evaluate(RequestProcess process) {
			string indexText = null; var index = 1;
			if (this.Index != null) indexText = this.Index.Evaluate(process);

			if (!string.IsNullOrWhiteSpace(indexText))
				index = int.Parse(indexText);

			return process.User.GetRequest(index);
		}

		public static Request FromXml(XmlNode node, AimlLoader loader) {
			// Search for XML attributes.
			XmlAttribute attribute;

			TemplateElementCollection index = null;

			attribute = node.Attributes["index"];
			if (attribute != null) index = new TemplateElementCollection(attribute.Value);

			// Search for properties in elements.
			foreach (XmlNode node2 in node.ChildNodes) {
				if (node2.NodeType == XmlNodeType.Element) {
					if (node2.Name.Equals("index", StringComparison.InvariantCultureIgnoreCase))
						index = TemplateElementCollection.FromXml(node2, loader);
				}
			}

			return new Request(index);
		}
	}
}
