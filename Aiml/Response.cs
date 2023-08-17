using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using Aiml.Media;

namespace Aiml;
public class Response(Request request, string text) {
	public Request Request { get; } = request;
	public Bot Bot => this.Request.Bot;
	public User User => this.Request.User;
	public string Text { get; private set; } = text;
	public IReadOnlyList<string> Sentences { get; private set; } = Array.AsReadOnly(request.Bot.SentenceSplit(text, true));

	public bool IsEmpty => string.IsNullOrWhiteSpace(this.Text);

	internal string ProcessOobElements() => this.Text = Regex.Replace(this.Text, @"<\s*oob\s*>.*?<(/?)\s*oob\s*>", m => {
		if (m.Groups[1].Value == "") {
			this.Bot.Log(LogLevel.Warning, "Cannot process nested <oob> elements.");
			return m.Value;
		}
		var xmlDocument = new XmlDocument();
		var builder = new StringBuilder();
		xmlDocument.LoadXml(m.Value);
		foreach (var childElement in xmlDocument.DocumentElement!.ChildNodes.OfType<XmlElement>()) {
			if (this.Bot.OobHandlers.TryGetValue(childElement.Name, out var handler))
				builder.Append(handler(childElement));
			else {
				this.Bot.Log(LogLevel.Warning, $"No handler found for <oob> element <{childElement.Name}>.");
				builder.Append(childElement.OuterXml);
			}
		}
		return builder.ToString();
	});

	/// <summary>Returns the last sentence of the response text.</summary>
	public string GetLastSentence() => this.GetLastSentence(1);
	/// <summary>Returns the <paramref name="n"/>th last sentence of the response text.</summary>
	public string GetLastSentence(int n) {
		return this.Sentences != null
			? this.Sentences[this.Sentences.Count - n]
			: throw new InvalidOperationException("Response is not finished.");
	}

	/// <summary>Parses rich media elements in this response and converts it to a list of <see cref="Message"/> instances</summary>
	public Message[] ToMessages() {
		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml($"<response>{this}</response>");

		var messages = new List<Message>();
		(List<IMediaElement> inlineElements, List<IMediaElement> blockElements)? currentMessage = null;
		var space = false;

		foreach (XmlNode node in xmlDocument.DocumentElement!.ChildNodes) {
			switch (node.NodeType) {
				case XmlNodeType.Whitespace:
					if (space || currentMessage == null) break;
					currentMessage.Value.inlineElements.Add(MediaText.Space);
					space = true;
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.SignificantWhitespace:
					var text = node.InnerText;
					currentMessage ??= new(new(), new());
					currentMessage.Value.inlineElements.Add(new MediaText(text));
					space = text.Length > 0 && char.IsWhiteSpace(text[^1]);
					break;
				default:
					if (node is XmlElement element) {
						if (this.Bot.MediaElements.TryGetValue(element.Name, out var data)) {
							if (data.type == MediaElementType.Separator) {
								if (currentMessage is not null)
									messages.Add(new(currentMessage.Value.inlineElements.ToArray(), currentMessage.Value.blockElements.ToArray(), data.reviver(this.Bot, element)));
								currentMessage = (new(), new());
							} else if (data.type == MediaElementType.Block) {
								currentMessage ??= new(new(), new());
								currentMessage.Value.blockElements.Add(data.reviver(this.Bot, element));
							} else {
								currentMessage ??= new(new(), new());
								currentMessage.Value.inlineElements.Add(data.reviver(this.Bot, element));
							}
						} else {
							// If we don't know what type of media element it is, treat it as an inline one.
							currentMessage ??= new(new(), new());
							currentMessage.Value.inlineElements.Add(new MediaElement(element));
						}
					}
					break;
			}
		}
		if (currentMessage is not null)
			messages.Add(new(currentMessage.Value.inlineElements.ToArray(), currentMessage.Value.blockElements.ToArray(), null));
		return messages.ToArray();
	}

	/// <summary>Returns the response text, with rich media elements in raw XML. Messages are separated with newlines.</summary>
	public override string ToString() => this.Text;
}
