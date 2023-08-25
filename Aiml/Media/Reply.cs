using System.Text.RegularExpressions;
using System.Xml.Linq;

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

	public static Reply FromXml(XElement element, Response response) {
		string? text = null, postback = null;
		foreach (var childElement in element.Elements()) {
			switch (childElement.Name.LocalName.ToLowerInvariant()) {
				case "text": text = childElement.Value; break;
				case "postback": if (!childElement.IsEmpty) postback = childElement.Value; break;
			}
		}
		if (text == null) {
			text = string.Join(null, from node in element.Nodes().OfType<XText>() select node.Value);
			text = Regex.Replace(text, @"\s+", " ");
		}
		return new(text, postback ?? text);
	}
}
