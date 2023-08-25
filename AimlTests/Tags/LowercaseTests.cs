using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class LowercaseTests {
	[Test]
	public void Evaluate() {
		var tag = new Lowercase(new("hello WORLD says I."));
		Assert.AreEqual("hello world says i.", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
