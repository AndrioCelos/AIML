using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using Aiml.Media;
using System.Xml.Linq;

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
		var element = XElement.Parse(m.Value);
		var builder = new StringBuilder();
		foreach (var childElement in element.Elements()) {
			if (AimlLoader.oobHandlers.TryGetValue(childElement.Name.LocalName, out var handler))
				builder.Append(handler(childElement));
			else
				this.Bot.Log(LogLevel.Warning, $"No handler found for <oob> element <{childElement.Name}>.");
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
		try {
			var element = XElement.Parse($"<response>{this}</response>");

			var messages = new List<Message>();
			(List<IMediaElement> inlineElements, List<IMediaElement> blockElements)? currentMessage = null;

			foreach (var node in element.Nodes()) {
				switch (node) {
					case XText textNode:
						var text = textNode.Value;
						currentMessage ??= new(new(), new());
						currentMessage.Value.inlineElements.Add(new MediaText(text));
						break;
					case XElement childElement:
						try {
							if (AimlLoader.mediaElements.TryGetValue(childElement.Name.LocalName, out var data)) {
								if (data.type == MediaElementType.Separator) {
									if (currentMessage is not null)
										messages.Add(new(currentMessage.Value.inlineElements.ToArray(), currentMessage.Value.blockElements.ToArray(), data.parser(childElement, this)));
									currentMessage = (new(), new());
								} else if (data.type == MediaElementType.Block) {
									currentMessage ??= new(new(), new());
									currentMessage.Value.blockElements.Add(data.parser(childElement, this));
								} else {
									currentMessage ??= new(new(), new());
									currentMessage.Value.inlineElements.Add(data.parser(childElement, this));
								}
							} else {
								// If we don't know what type of media element it is, treat it as an inline one.
								currentMessage ??= new(new(), new());
								currentMessage.Value.inlineElements.Add(new MediaElement(element));
							}
						} catch (ArgumentException ex) {
							this.Bot.Log(LogLevel.Warning, $"Invalid <{childElement.Name.LocalName}> media element in response: {ex.Message}");
						}
						break;
				}
			}
			if (currentMessage is not null)
				messages.Add(new(currentMessage.Value.inlineElements.ToArray(), currentMessage.Value.blockElements.ToArray(), null));
			return messages.ToArray();
		} catch (XmlException ex) {
			this.Bot.Log(LogLevel.Warning, $"Failed to parse response media elements: {ex.Message}");
			return new Message[] { new(new[] { new MediaText(this.Text) }, Array.Empty<IMediaElement>(), null) };
		}
	}

	/// <summary>Returns the response text, with rich media elements in raw XML. Messages are separated with newlines.</summary>
	public override string ToString() => this.Text;
}
