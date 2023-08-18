using System.Xml;

namespace Aiml.Media;

/// <summary>The base class for rich media elements that present lists.</summary>
public abstract class ListBase(List<IReadOnlyList<IMediaElement>> items) : IMediaElement {
	public IReadOnlyList<IReadOnlyList<IMediaElement>> Items { get; } = items.AsReadOnly();

	protected static List<IReadOnlyList<IMediaElement>> ParseItems(XmlElement element) {
		var items = new List<IReadOnlyList<IMediaElement>>();
		foreach (var childElement in element.ChildNodes.OfType<XmlElement>()) {
			switch (childElement.Name.ToLowerInvariant()) {
				case "item": case "li": items.Add(MediaElement.ParseInlineElements(element).AsReadOnly()); break;
				default: throw new AimlException($"Unknown attribute {childElement.Name} in list element.");
			}
		}
		return items;
	}
}
