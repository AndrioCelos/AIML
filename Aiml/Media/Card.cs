using System.Xml;

namespace Aiml.Media;
/// <summary>A block-level rich media element that combines a title, subtitle, <see cref="Image"/>, and zero or more <see cref="Button"/> elements.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
/// <seealso cref="Carousel"/>
public class Card(string? imageUrl, string? title, string? subtitle, List<Button>? buttons) : IMediaElement {
	public string? ImageUrl { get; } = imageUrl;
	public string? Title { get; } = title;
	public string? Subtitle { get; } = subtitle;
	public IReadOnlyList<Button> Buttons { get; } = buttons?.AsReadOnly() ?? Array.AsReadOnly(Array.Empty<Button>());

	public static Card FromXml(XmlElement element) {
		string? imageUrl = null, title = null, subtitle = null;
		List<Button>? buttons = null;
		foreach (var childElement in element.ChildNodes.OfType<XmlElement>()) {
			switch (childElement.Name.ToLowerInvariant()) {
				case "image": imageUrl = childElement.InnerText; break;
				case "title": title = childElement.InnerText; break;
				case "subtitle": subtitle = childElement.InnerText; break;
				case "button":
					buttons ??= new();
					buttons.Add(Button.FromXml(childElement));
					break;
				default: throw new AimlException($"Unknown attribute {childElement.Name} in <card> element.");
			}
		}
		return new(imageUrl, title, subtitle, buttons);
	}
}
