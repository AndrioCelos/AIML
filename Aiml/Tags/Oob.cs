using System.Text;
using System.Xml;

namespace Aiml.Tags;
/// <summary>Represents an out-of-band or rich media tag. These are not fully parsed during template processing.</summary>
/// <remarks>
///		<para><c>oob</c> and rich media tags provide out-of-band instructions to the innterpreter.</para>
///		<para>The <c>oob</c> element is defined by the AIML 2.0 specification.
///			Rich media elements are defined by the AIML 2.1 draft specification.</para>
/// </remarks>
public sealed class Oob(string name, string? attributes, TemplateElementCollection? children) : RecursiveTemplateTag(children) {
	public string Name { get; } = name;
	public string? Attributes { get; } = attributes;

	public override string Evaluate(RequestProcess process)
		=> this.Children != null
			? $"<{this.Name}{this.Attributes}>{this.Children?.Evaluate(process) ?? ""}</{this.Name}>"
			: $"<{this.Name}{this.Attributes}/>";

	public static Oob FromXml(XmlNode node, AimlLoader loader, params string[] childTags) {
		var builder = new StringBuilder();
		foreach (var attribute in node.Attributes.Cast<XmlAttribute>()) {
			builder.Append(' ');
			builder.Append(attribute.OuterXml);
		}

		var oldFC = loader.ForwardCompatible;
		if (node.HasChildNodes) {
			loader.ForwardCompatible = true;
			var children = new List<TemplateNode>();
			foreach (XmlNode node2 in node.ChildNodes) {
				if (node2.NodeType == XmlNodeType.Whitespace) {
					children.Add(new TemplateText(" "));
				} else if (node2.NodeType is XmlNodeType.Text or XmlNodeType.SignificantWhitespace) {
					children.Add(new TemplateText(node2.InnerText));
				} else if (node2.NodeType == XmlNodeType.Element) {
					if (Array.IndexOf(childTags, node2.Name) >= 0) {
						children.Add(FromXml(node2, loader, node2.Name.Equals("card", StringComparison.InvariantCultureIgnoreCase) ? new[] { "title", "subtitle", "image", "button" } :
							node2.Name.Equals("button", StringComparison.InvariantCultureIgnoreCase) ? new[] { "text", "postback", "url" } : Array.Empty<string>()));
					} else
						children.Add(loader.ParseElement(node2));
				}
			}
			loader.ForwardCompatible = oldFC;
			return new Oob(node.Name, builder.ToString(), new TemplateElementCollection(children.ToArray()));
		} else
			return new Oob(node.Name, builder.ToString(), null);
	}
}
