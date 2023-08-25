using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class ExplodeTests {
	[Test]
	public void EvaluateWithSpaces() {
		var tag = new Explode(new("Hello world"));
		Assert.AreEqual("H e l l o w o r l d", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithPunctuation() {
		var tag = new Explode(new("1.5"));
		Assert.AreEqual("1 5", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
