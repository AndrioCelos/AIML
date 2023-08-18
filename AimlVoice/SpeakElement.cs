using System.Xml;
using Aiml;

namespace AimlVoice;
internal class SpeakElement(XmlElement ssml, string altText) : IMediaElement {
	public XmlElement SSML { get; } = ssml;
	public string AltText { get; } = altText;

	public static SpeakElement FromXml(XmlElement element) {
		if (!element.HasAttribute("version"))
			element.SetAttribute("version", "1.0");
		if (!element.HasAttribute("xml:lang"))
			element.SetAttribute("xml:lang", Program.bot!.Config.Locale.Name.ToLowerInvariant());

		var node = element.ChildNodes.OfType<XmlElement>().FirstOrDefault(el => el.Name.Equals("alt", StringComparison.OrdinalIgnoreCase));
		string? altText;
		if (node is not null) {
			altText = node.InnerText;
			element.RemoveChild(node);
		} else
			altText = element.InnerText;

		return new(element, altText);
	}
}
