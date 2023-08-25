using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class ThatTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.Bot.Config.ThatExcludeEmptyResponse = true;
		test.User.That = "Hello again.";
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello world. This is a test."));
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello again."));
		test.User.Responses.Add(new(new("", test.User, test.Bot), ""));
		return test;
	}

	[Test]
	public void ParseWithIndex() {
		var tag = new That(new("1,2"));
		Assert.AreEqual("1,2", tag.Index?.ToString());
	}

	[Test]
	public void ParseWithDefault() {
		var tag = new That(null);
		Assert.IsNull(tag.Index);
	}

	[Test]
	public void EvaluateWithIndex() {
		var test = GetTest();
		Assert.AreEqual("Hello again.", new That(new("2,1")).Evaluate(test.RequestProcess).ToString());
		Assert.AreEqual("This is a test.", new That(new("3,1")).Evaluate(test.RequestProcess).ToString());
		Assert.AreEqual("Hello world.", new That(new("3,2")).Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithWhitespace() {
		var test = GetTest();
		Assert.AreEqual("Hello again.", new That(new(" 2 ,\n\t1 ")).Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithResponseOutOfRange() {
		var test = GetTest();
		Assert.AreEqual(test.Bot.Config.DefaultHistory, new That(new("4,1")).Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithSentenceOutOfRange() {
		var test = GetTest();
		Assert.AreEqual(test.Bot.Config.DefaultHistory, new That(new("1,2")).Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithDefault() {
		Assert.AreEqual("Hello again.", new That(null).Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithInvalidIndex() {
		var test = GetTest();
		Assert.AreEqual(test.Bot.Config.DefaultHistory, test.AssertWarning(() => new That(new("1")).Evaluate(test.RequestProcess).ToString()));
	}

	[Test]
	public void EvaluateWithInvalidResponse() {
		var test = GetTest();
		Assert.AreEqual(test.Bot.Config.DefaultHistory, test.AssertWarning(() => new That(new("0,1")).Evaluate(test.RequestProcess).ToString()));
	}

	[Test]
	public void EvaluateWithInvalidSentence() {
		var test = GetTest();
		Assert.AreEqual(test.Bot.Config.DefaultHistory, test.AssertWarning(() => new That(new("1,0")).Evaluate(test.RequestProcess).ToString()));
	}
}
