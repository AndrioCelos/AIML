using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class RequestTests {
	[Test]
	public void Parse_Index() {
		var tag = new Aiml.Tags.Request(new("2"));
		Assert.AreEqual("2", tag.Index?.ToString());
	}

	[Test]
	public void Parse_Default() {
		var tag = new Aiml.Tags.Request(null);
		Assert.IsNull(tag.Index);
	}

	[Test]
	public void Evaluate_Index() {
		var test = new AimlTest();
		test.User.Requests.Add(new("Hello world. This is a test.", test.User, test.Bot));
		test.User.Requests.Add(new("Hello again.", test.User, test.Bot));
		test.User.Requests.Add(new("current request", test.User, test.Bot));

		var tag = new Aiml.Tags.Request(new("2"));
		Assert.AreEqual("Hello world. This is a test.", tag.Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void Evaluate_Default() {
		var test = new AimlTest();
		test.User.Requests.Add(new("Hello world. This is a test.", test.User, test.Bot));
		test.User.Requests.Add(new("Hello again.", test.User, test.Bot));
		test.User.Requests.Add(new("current request", test.User, test.Bot));

		var tag = new Aiml.Tags.Request(null);
		Assert.AreEqual("Hello again.", tag.Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void Evaluate_IndexOutOfRange() {
		var test = new AimlTest();
		var tag = new Aiml.Tags.Request(null);
		Assert.AreEqual(test.Bot.Config.DefaultHistory, tag.Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void Evaluate_InvalidIndex() {
		var test = new AimlTest();
		var tag = new Aiml.Tags.Request(new("foo"));
		Assert.AreEqual(test.Bot.Config.DefaultHistory, test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()));
	}
}
