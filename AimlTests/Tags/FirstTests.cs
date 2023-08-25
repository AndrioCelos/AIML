using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class FirstTests {
	[Test]
	public void EvaluateWithZeroWords() {
		var tag = new First(new(""));
		Assert.AreEqual("nil", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithOneWord() {
		var tag = new First(new("1"));
		Assert.AreEqual("1", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithMultipleWords() {
		var tag = new First(new("1 2 3"));
		Assert.AreEqual("1", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
