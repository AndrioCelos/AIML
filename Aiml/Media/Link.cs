using System.Text.RegularExpressions;
using System.Xml;

namespace Aiml.Media;
/// <summary>An inline rich media element that presents a hyperlink.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
public class Link(string text, string url) : IMediaElement {
	public string Text { get; } = text;
	public string Url { get; } = url;

	public static Link FromXml(XmlElement element) {
		string? text = null, url = null;
		foreach (var childElement in element.ChildNodes.OfType<XmlElement>()) {
			switch (childElement.Name.ToLowerInvariant()) {
				case "text": text = childElement.InnerText; break;
				case "url": url = childElement.InnerText; break;
			}
		}
		if (url == null) {
			url = string.Join(null, from XmlNode node in element.ChildNodes where node is XmlCharacterData and not XmlComment select node.Value);
			url = Regex.Replace(url, @"\s+", "");
		}
		return new(text ?? url, url);
	}
}
