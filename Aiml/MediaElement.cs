using System.Xml;

namespace Aiml;
public class MediaElement(XmlElement element) : IMediaElement {
	public XmlElement Element { get; } = element;

	internal static List<IMediaElement> ParseInlineElements(XmlElement element) {
		var elements = new List<IMediaElement>();
		var space = false;

		foreach (XmlNode node in element.ChildNodes) {
			switch (node.NodeType) {
				case XmlNodeType.Whitespace:
					if (space) break;
					elements.Add(MediaText.Space);
					space = true;
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.SignificantWhitespace:
					var text = node.InnerText;
					elements.Add(new MediaText(text));
					space = text.Length > 0 && char.IsWhiteSpace(text[^1]);
					break;
				default:
					if (node is XmlElement childElement && !childElement.Name.Equals("oob", StringComparison.OrdinalIgnoreCase)) {
						if (AimlLoader.mediaElements.TryGetValue(childElement.Name, out var data)) {
							if (data.type != MediaElementType.Inline) throw new AimlException($"<{childElement.Name}> element is not valid within <{element.Name}> element.");
							elements.Add(data.parser(childElement));
						} else {
							// If we don't know what type of media childElement it is, treat it as an inline one.
							elements.Add(new MediaElement(childElement));
						}
					}
					break;
			}
		}
		return elements;
	}
}
