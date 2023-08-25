using System.Xml.Linq;

namespace Aiml;
public class MediaElement(XElement element) : IMediaElement {
	public XElement Element { get; } = element;

	internal static List<IMediaElement> ParseInlineElements(XElement element, Response response) {
		var elements = new List<IMediaElement>();

		foreach (var node in element.Nodes()) {
			switch (node) {
				case XText textNode:
					elements.Add(new MediaText(textNode.Value));
					break;
				case XElement childElement:
					if (AimlLoader.mediaElements.TryGetValue(childElement.Name.LocalName, out var data)) {
						if (data.type != MediaElementType.Inline) throw new AimlException($"{data.type} element <{childElement.Name.LocalName}> is not valid here", element);
						elements.Add(data.parser(childElement, response));
					} else {
						// If we don't know what type of media childElement it is, treat it as an inline one.
						elements.Add(new MediaElement(childElement));
					}
					break;
			}
		}
		return elements;
	}
}
