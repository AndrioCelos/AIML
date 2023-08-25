using System.Xml.Linq;

namespace Aiml.Media;

/// <summary>The base class for rich media elements that present lists.</summary>
public abstract class ListBase(List<IReadOnlyList<IMediaElement>> items) : IMediaElement {
	public IReadOnlyList<IReadOnlyList<IMediaElement>> Items { get; } = items.AsReadOnly();

	protected static List<IReadOnlyList<IMediaElement>> ParseItems(XElement element, Response response) {
		var items = new List<IReadOnlyList<IMediaElement>>();
		foreach (var childElement in element.Elements()) {
			switch (childElement.Name.LocalName.ToLowerInvariant()) {
				case "item": case "li": items.Add(MediaElement.ParseInlineElements(element, response).AsReadOnly()); break;
				default: throw new AimlException($"Unknown attribute {childElement.Name}", element);
			}
		}
		return items;
	}
}
