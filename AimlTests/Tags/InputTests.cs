using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class InputTests {
	[Test]
	public void ParseWithIndex() {
		var tag = new Input(new("2"));
		Assert.AreEqual("2", tag.Index?.ToString());
	}

	[Test]
	public void ParseWithDefault() {
		var tag = new Input(null);
		Assert.IsNull(tag.Index);
	}

	[Test]
	public void EvaluateWithIndex() {
		var test = new AimlTest();
		test.User.Requests.Add(new("Hello world. This is a test.", test.User, test.Bot));
		test.User.Requests.Add(new("Hello again.", test.User, test.Bot));

		var tag = new Input(new("2"));
		Assert.AreEqual("This is a test", tag.Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithDefault() {
		var test = new AimlTest();
		test.User.Requests.Add(new("Hello world. This is a test.", test.User, test.Bot));
		test.User.Requests.Add(new("Hello again.", test.User, test.Bot));

		var tag = new Input(null);
		Assert.AreEqual("Hello again", tag.Evaluate(test.RequestProcess).ToString());
	}
}
