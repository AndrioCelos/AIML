using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class SentenceTests {
	[Test]
	public void Evaluate() {
		var tag = new Sentence(new("hello WORLD says I."));
		Assert.AreEqual("Hello WORLD says I.", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
