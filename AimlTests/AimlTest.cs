using System.Xml;

namespace Aiml.Tests;
internal class AimlTest {
	public bool ExpectingWarning { get; set; }
	public Bot Bot { get; }
	public User User { get; }
	public RequestProcess RequestProcess { get; }

	public string TestTemplate {
		set {
			var document = new XmlDocument();
			document.LoadXml($"<aiml version='2.1'><category><pattern>TEST</pattern><template>{value}</template></category></aiml>");
			this.Bot.AimlLoader.LoadAIML(document, "test.aiml");
		}
	}

	public AimlTest() : this(new Bot()) { }
	public AimlTest(Bot bot) {
		this.Bot = bot;
		this.User = new("tester", this.Bot);
		this.RequestProcess = new(new(new("TEST", this.User, this.Bot), "TEST"), 0, false);

		this.Bot.LogMessage += this.Bot_LogMessage;
	}
	public AimlTest(Random random) : this(new Bot(random)) { }
	public AimlTest(string sampleRequestSentenceText) : this() {
		this.RequestProcess = new(new(new(sampleRequestSentenceText, this.User, this.Bot), sampleRequestSentenceText), 0, false);
	}

	public Response Chat() => this.Chat("TEST");
	public Response Chat(string requestText) {
		var response = this.Bot.Chat(new(requestText, this.User, this.Bot));
		if (this.ExpectingWarning)
			Assert.Fail("Expected warning was not raised.");
		return response;
	}

	public TResult AssertWarning<TResult>(Func<TResult> f) {
		this.ExpectingWarning = true;
		var result = f();
		if (this.ExpectingWarning)
			Assert.Fail("Expected warning was not raised.");
		return result;
	}

	private void Bot_LogMessage(object? sender, LogMessageEventArgs e) {
		if (e.Level == LogLevel.Warning) {
			if (this.ExpectingWarning)
				this.ExpectingWarning = false;
			else
				Assert.Fail($"AIML request raised a warning: {e.Message}");
		}
	}

	internal static Template GetTemplate(PatternNode root, params string[] pathTokens) {
		var node = root;
		foreach (var token in pathTokens) {
			if (!node.Children.TryGetValue(token, out node)) {
				Assert.Fail($"Node '{token}' was not found.");
				throw new KeyNotFoundException("No match");
			}
		}
		if (node.Template is not null) return node.Template;
		Assert.Fail($"Node '{string.Join(' ', pathTokens)} is not a leaf.");
		throw new KeyNotFoundException("No match");
	}

	internal static XmlDocument ParseXmlDocument(string xml) {
		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(xml);
		return xmlDocument;
	}
	internal static XmlElement ParseXmlElement(string xml) => ParseXmlDocument(xml).DocumentElement!;
}
