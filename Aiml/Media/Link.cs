using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Aiml.Media;
/// <summary>An inline rich media element that presents a hyperlink.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
public class Link(string text, string url) : IMediaElement {
	public string Text { get; } = text;
	public string Url { get; } = url;

	public static Link FromXml(XElement element, Response response) {
		string? text = null, url = null;
		foreach (var childElement in element.Elements()) {
			switch (childElement.Name.LocalName.ToLowerInvariant()) {
				case "text": text = childElement.Value; break;
				case "url": url = childElement.Value; break;
				default: throw new AimlException($"Unknown attribute {childElement.Name}", element);
			}
		}
		if (url == null) {
			url = string.Join(null, from node in element.Nodes().OfType<XText>() select node.Value);
			url = Regex.Replace(url, @"\s+", "");
		}
		return new(text ?? url, url);
	}
}
