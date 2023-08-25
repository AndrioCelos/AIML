namespace Aiml.Tests;
/// <summary>Creates a mock setup for AIML tests.</summary>
internal class AimlTest {
	public Bot Bot { get; }
	public User User { get; }
	public RequestProcess RequestProcess { get; }

	private bool expectingWarning;

	/// <summary>Initialises a new <see cref="AimlTest"/> with a new bot with the default settings.</summary>
	public AimlTest() : this(new Bot()) { }
	/// <summary>Initialises a new <see cref="AimlTest"/> from the specified <see cref="Bot"/>.</summary>
	public AimlTest(Bot bot) {
		this.Bot = bot;
		this.User = new("tester", this.Bot);
		this.RequestProcess = new(new(new("TEST", this.User, this.Bot), "TEST"), 0, false);

		this.Bot.LogMessage += this.Bot_LogMessage;
	}
	/// <summary>Initialises a new <see cref="AimlTest"/> using the specified <see cref="Random"/>.</summary>
	public AimlTest(Random random) : this(new Bot(random)) { }
	/// <summary>Initialises a new <see cref="AimlTest"/> using the specified sample request.</summary>
	public AimlTest(string sampleRequestSentenceText) : this()
		=> this.RequestProcess = new(new(new(sampleRequestSentenceText, this.User, this.Bot), sampleRequestSentenceText), 0, false);

	/// <summary>Asserts that the specified method causes a warning message to be logged.</summary>
	public void AssertWarning(Action action) {
		this.expectingWarning = true;
		action();
		if (this.expectingWarning)
			Assert.Fail("Expected warning was not raised.");
	}
	/// <summary>Asserts that the specified function causes a warning message to be logged.</summary>
	/// <returns>The return value of <paramref name="f"/>.</returns>
	public TResult AssertWarning<TResult>(Func<TResult> f) {
		this.expectingWarning = true;
		var result = f();
		if (this.expectingWarning)
			Assert.Fail("Expected warning was not raised.");
		return result;
	}

	private void Bot_LogMessage(object? sender, LogMessageEventArgs e) {
		if (e.Level == LogLevel.Warning) {
			if (this.expectingWarning)
				this.expectingWarning = false;
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
}
