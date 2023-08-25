using System.Xml.Linq;

namespace Aiml.Media;
/// <summary>A block-level rich media element that combines a title, subtitle, <see cref="Image"/>, and zero or more <see cref="Button"/> elements.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
/// <seealso cref="Carousel"/>
public class Card(string? imageUrl, string? title, string? subtitle, List<Button>? buttons) : IMediaElement {
	public string? ImageUrl { get; } = imageUrl;
	public string? Title { get; } = title;
	public string? Subtitle { get; } = subtitle;
	public IReadOnlyList<Button> Buttons { get; } = buttons?.AsReadOnly() ?? Array.AsReadOnly(Array.Empty<Button>());

	public static Card FromXml(XElement element, Response response) {
		string? imageUrl = null, title = null, subtitle = null;
		List<Button>? buttons = null;
		foreach (var childElement in element.Elements()) {
			switch (childElement.Name.LocalName.ToLowerInvariant()) {
				case "image": imageUrl = childElement.Value; break;
				case "title": title = childElement.Value; break;
				case "subtitle": subtitle = childElement.Value; break;
				case "button":
					buttons ??= new();
					buttons.Add(Button.FromXml(childElement, response));
					break;
				default: throw new AimlException($"Unknown attribute {childElement.Name}", element);
			}
		}
		return new(imageUrl, title, subtitle, buttons);
	}
}
