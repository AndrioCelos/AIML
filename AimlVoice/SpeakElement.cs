using System.Xml.Linq;
using Aiml;

namespace AimlVoice;
internal class SpeakElement(XElement ssml, string altText) : IMediaElement {
	public XElement SSML { get; } = ssml;
	public string AltText { get; } = altText;

	public static SpeakElement FromXml(XElement element, Response response) {
		if (element.Attribute("version") is null)
			element.SetAttributeValue("version", "1.0");
		if (element.Attribute("xml:lang") is null)
			element.SetAttributeValue("xml:lang", response.Bot.Config.Locale.Name.ToLowerInvariant());

		var node = element.Elements().FirstOrDefault(el => el.Name.LocalName.Equals("alt", StringComparison.OrdinalIgnoreCase));
		string? altText;
		if (node is not null) {
			altText = node.Value;
			node.Remove();
		} else
			altText = element.Value;

		return new(element, altText);
	}
}
