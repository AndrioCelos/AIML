using System.Text.RegularExpressions;
using System.Xml;

namespace Aiml.Media;
/// <summary>A fragment of a response that should be presented as a single message, represented as a collection of rich media elements.</summary>
public class Message(IMediaElement[] inlineElements, IMediaElement[] blockElements, IMediaElement? separator) {
	public IReadOnlyList<IMediaElement> InlineElements { get; } = Array.AsReadOnly(inlineElements);
	public IReadOnlyList<IMediaElement> BlockElements { get; } = Array.AsReadOnly(blockElements);
	public IMediaElement? Separator { get; } = separator;

	internal static (string text, string postback) ParsePostbackExpression(XmlElement element) {
		string? text = null, postback = null;
		foreach (var childElement in element.ChildNodes.OfType<XmlElement>()) {
			switch (childElement.Name.ToLowerInvariant()) {
				case "text": text = childElement.InnerText; break;
				case "postback": if (childElement.HasChildNodes) postback = childElement.InnerText; break;
			}
		}
		if (text == null) {
			text = string.Join(null, from XmlNode node in element.ChildNodes where node is XmlCharacterData and not XmlComment select node.Value);
			text = Regex.Replace(text, @"\s+", " ");
		}
		return (text, postback ?? text);
	}
}
