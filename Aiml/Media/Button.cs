using System.Text.RegularExpressions;
using System.Xml;

namespace Aiml.Media;
/// <summary>A block-level rich media element that presents a button that either links to a URL or sends a postback to the bot.</summary>
/// <remarks>
///		<para>When a postback button is selected, its text should be displayed as the resulting request. If the postback is different, it should not be displayed.</para>
///		<para>This element is defined by the AIML 2.1 specification.</para>
///	</remarks>
///	<seealso cref="Reply"/>
public class Button(string text, string? postback, string? url) : IMediaElement {
	public string Text { get; } = text;
	public string? Postback { get; } = postback;
	public string? Url { get; } = url;

	public static Button FromXml(XmlElement element) {
		string? text = null, postback = null, url = null;
		var hasPostback = false;
		foreach (var childElement in element.ChildNodes.OfType<XmlElement>()) {
			switch (childElement.Name.ToLowerInvariant()) {
				case "text": text = childElement.InnerText; break;
				case "postback": hasPostback = true; if (childElement.HasChildNodes) postback = childElement.InnerText; break;
				case "url": url = childElement.InnerText; break;
			}
		}
		if (hasPostback && url is not null) throw new AimlException("<button> element cannot have both postback and url attributes.");
		if (text == null) {
			text = string.Join(null, from XmlNode node in element.ChildNodes where node is XmlCharacterData and not XmlComment select node.Value);
			text = Regex.Replace(text, @"\s+", " ");
		}
		return new(text, url is null ? (postback ?? text) : null, url);
	}
}
