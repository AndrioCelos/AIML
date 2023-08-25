using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class RestTests {
	[Test]
	public void EvaluateWithZeroWords() {
		var tag = new Rest(new(""));
		Assert.AreEqual("nil", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithOneWord() {
		var tag = new Rest(new("1"));
		Assert.AreEqual("nil", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithMultipleWords() {
		var tag = new Rest(new("1 2 3"));
		Assert.AreEqual("2 3", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
