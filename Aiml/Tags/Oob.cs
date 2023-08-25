using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Aiml.Tags;
/// <summary>Represents an out-of-band or rich media tag. These are not fully parsed during template processing.</summary>
/// <remarks>
///		<para><c>oob</c> and rich media tags provide out-of-band instructions to the innterpreter.</para>
///		<para>The <c>oob</c> element is defined by the AIML 2.0 specification.
///			Rich media elements are defined by the AIML 2.1 draft specification.</para>
/// </remarks>
public sealed class Oob(string name, IEnumerable<XAttribute> attributes, TemplateElementCollection children) : RecursiveTemplateTag(children) {
	private readonly XAttribute[] attributes = attributes.ToArray();

	public string Name { get; } = name;
	public IReadOnlyList<XAttribute> Attributes => Array.AsReadOnly(this.attributes);

	public override string Evaluate(RequestProcess process) {
		var builder = new StringBuilder();
		using var writer = XmlWriter.Create(builder, new() { OmitXmlDeclaration = true });
		writer.WriteStartElement(this.Name);
		foreach (var attr in this.attributes)
			writer.WriteAttributeString(attr.Name.LocalName, attr.Value);
		writer.WriteRaw(this.EvaluateChildren(process));
		writer.WriteEndElement();
		writer.Flush();
		return builder.ToString();
	}

	public static Oob FromXml(XElement el, AimlLoader loader) => FromXml(el, loader, null);
	public static Oob FromXml(XElement el, AimlLoader loader, params string[]? childElements) {
		var oldFC = loader.ForwardCompatible;
		loader.ForwardCompatible = true;
		var children = new List<TemplateNode>();
		foreach (var childNode in el.Nodes()) {
			switch (childNode) {
				case XText textNode:
					children.Add(new TemplateText(textNode.Value));
					break;
				case XElement childElement:
					if (childElements is not null) {
						if (!childElements.Contains(childElement.Name.LocalName, StringComparer.OrdinalIgnoreCase))
							throw new AimlException($"Invalid child element <{childElement.Name}>", el);
						AimlLoader.mediaElements.TryGetValue(childElement.Name.LocalName, out var childData);
						children.Add(FromXml(childElement, loader, childData.childElements));
					} else
						children.Add(loader.ParseElement(childElement));
					break;
			}
		}
		loader.ForwardCompatible = oldFC;
		return new Oob(el.Name.LocalName, el.Attributes(), new TemplateElementCollection(children));
	}
}
