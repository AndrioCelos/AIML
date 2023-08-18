using System.Xml;

namespace Aiml.Media;
/// <summary>A block-level rich media element that presents a menu of one or more <see cref="Card"/> elements.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
public class Carousel(List<Card> cards) : IMediaElement {
	public IReadOnlyList<Card> Cards { get; } = cards.AsReadOnly();

	public static Carousel FromXml(XmlElement element) {
		var cards = new List<Card>();
		foreach (var childElement in element.ChildNodes.OfType<XmlElement>()) {
			switch (childElement.Name.ToLowerInvariant()) {
				case "card": cards.Add(Card.FromXml(childElement)); break;
				default: throw new AimlException($"Unknown attribute {childElement.Name} in <carousel> element.");
			}
		}
		return new(cards);
	}
}
