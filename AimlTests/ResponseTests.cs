using Aiml.Media;

namespace Aiml.Tests;

[TestFixture]
public class ResponseTests {
	[Test]
	public void GetLastSentenceTest() {
		var subject = new Response(new AimlTest().RequestProcess.Sentence.Request, "Hello, world! This is a test.");
		Assert.AreEqual("This is a test.", subject.GetLastSentence());
		Assert.AreEqual("This is a test.", subject.GetLastSentence(1));
		Assert.AreEqual("Hello, world!", subject.GetLastSentence(2));
	}

	[Test]
	public void ToMessages_Button_PostbackTextOnly() {
		var subject = new Response(new AimlTest().RequestProcess.Sentence.Request, "Hello, world!<split/><list><item>This is a test.</item></list><button>Hello!</button>");
		var messages = subject.ToMessages();
		Assert.AreEqual(2, messages.Length);

		Assert.AreEqual(1, messages[0].InlineElements.Count);
		Assert.IsInstanceOf<MediaText>(messages[0].InlineElements[0]);
		Assert.AreEqual("Hello, world!", ((MediaText) messages[0].InlineElements[0]).Text);
		Assert.IsInstanceOf<Split>(messages[0].Separator);

		Assert.AreEqual(1, messages[1].InlineElements.Count);
		Assert.IsInstanceOf<Media.List>(messages[1].InlineElements[0]);
		Assert.AreEqual(1, messages[1].BlockElements.Count);
		Assert.IsInstanceOf<Button>(messages[1].BlockElements[0]);
		Assert.IsNull(messages[1].Separator);
	}

	[Test]
	public void ToMessages() {
		var subject = new Response(new AimlTest().RequestProcess.Sentence.Request, "Hello, world! <button>Hello!</button>");
		var messages = subject.ToMessages();
		Assert.IsInstanceOf<Button>(messages[0].BlockElements[0]);
		Assert.AreEqual("Hello!", ((Button) messages[0].BlockElements[0]).Text);
		Assert.AreEqual("Hello!", ((Button) messages[0].BlockElements[0]).Postback);
		Assert.IsNull(((Button) messages[0].BlockElements[0]).Url);
	}
}
