using System.Text;
using System.Xml;

namespace Aiml.Tags;
/// <summary>Represents an out-of-band or rich media tag. These are not fully parsed during template processing.</summary>
/// <remarks>
///		<para><c>oob</c> and rich media tags provide out-of-band instructions to the innterpreter.</para>
///		<para>The <c>oob</c> element is defined by the AIML 2.0 specification.
///			Rich media elements are defined by the AIML 2.1 draft specification.</para>
/// </remarks>
public sealed class Oob(string name, string attributes, TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public string Name { get; } = name;
	public string Attributes { get; } = attributes;

	public override string Evaluate(RequestProcess process)
		=> this.Children != null
			? $"<{this.Name}{this.Attributes}>{this.EvaluateChildren(process)}</{this.Name}>"
			: $"<{this.Name}{this.Attributes}/>";

	public static Oob FromXml(XmlElement el, AimlLoader loader, params string[] childTags) {
		var builder = new StringBuilder();
		foreach (var attribute in el.Attributes.Cast<XmlAttribute>()) {
			builder.Append(' ');
			builder.Append(attribute.OuterXml);
		}

		var oldFC = loader.ForwardCompatible;
		if (el.HasChildNodes) {
			loader.ForwardCompatible = true;
			var children = new List<TemplateNode>();
			foreach (XmlNode childNode in el.ChildNodes) {
				if (childNode.NodeType == XmlNodeType.Whitespace) {
					children.Add(new TemplateText(" "));
				} else if (childNode.NodeType is XmlNodeType.Text or XmlNodeType.SignificantWhitespace) {
					children.Add(new TemplateText(childNode.InnerText));
				} else if (childNode is XmlElement childElement) {
					if (Array.IndexOf(childTags, childElement.Name) >= 0) {
						children.Add(FromXml(childElement, loader, childElement.Name.Equals("card", StringComparison.InvariantCultureIgnoreCase) ? new[] { "title", "subtitle", "image", "button" } :
							childElement.Name.Equals("button", StringComparison.InvariantCultureIgnoreCase) ? new[] { "text", "postback", "url" } : Array.Empty<string>()));
					} else if (childTags.Length == 0)
						children.Add(loader.ParseElement(childElement));
					else
						throw new AimlException($"<{childElement.Name}> is not a valid child of <{el.Name}>.");
				}
			}
			loader.ForwardCompatible = oldFC;
			return new Oob(el.Name, builder.ToString(), new TemplateElementCollection(children.ToArray()));
		} else
			return new Oob(el.Name, builder.ToString(), TemplateElementCollection.Empty);
	}
}
