using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Returns the value of a bot property.</summary>
	/// <remarks>
	///		<para>This element has the following attribute:</para>
	///		<list type="table">
	///			<item>
	///				<term><c>name</c></term>
	///				<description>the property to get.</description>
	///			</item>
	///		</list>
	///		<para>This element has no content.</para>
	///		<para>This element is defined by the AIML 1.1 specification.</para>
	/// </remarks>
	public sealed class Bot(TemplateElementCollection key) : TemplateNode {
		public TemplateElementCollection Key { get; private set; } = key;

		public override string Evaluate(RequestProcess process) => process.Bot.GetProperty(this.Key.Evaluate(process));

		public static Bot FromXml(XmlNode node, AimlLoader loader) {
			// Search for XML attributes.
			XmlAttribute attribute;

			TemplateElementCollection? key = null;

			attribute = node.Attributes["name"];
			if (attribute != null) key = new TemplateElementCollection(attribute.Value);

			// Search for properties in elements.
			foreach (XmlNode node2 in node.ChildNodes) {
				if (node2.NodeType == XmlNodeType.Element) {
					if (node2.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase)) {
						key = TemplateElementCollection.FromXml(node2, loader);
					}
				}
			}

			if (key == null) throw new AimlException("bot element is missing a name property.");

			return new Bot(key);
		}
	}
}
