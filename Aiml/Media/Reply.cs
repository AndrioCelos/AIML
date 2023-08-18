using System.Xml;

namespace Aiml.Media;
/// <summary>
/// A block-level rich media element that presents a suggested reply to the message.
/// Unlike a <see cref="Button"/>, it should be hidden after another request is sent.
/// </summary>
/// <remarks>
///		<para>When a reply is selected, its text should be displayed as the resulting request. If the postback is different, it should not be displayed.</para>
///		<para>This element is defined by the AIML 2.1 specification.</para>
///	</remarks>
public class Reply(string text, string postback) : IMediaElement {
	public string Text { get; } = text;
	public string Postback { get; } = postback;

	public static Reply FromXml(XmlElement el) {
		string? text = null, postback = null;

		foreach (var el2 in el.ChildNodes.OfType<XmlElement>()) {
			switch (el2.Name.ToLowerInvariant()) {
				case "text": text = el2.InnerText; break;
				case "postback": postback = el2.InnerText; break;
			}
		}
		if (text == null && postback == null)
			text = postback = el.InnerText;
		return new(text ?? "", postback ?? text ?? "");
	}
}
