using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class FormalTests {
	[Test]
	public void Evaluate() {
		var tag = new Formal(new("hello WORLD says I."));
		Assert.AreEqual("Hello World Says I.", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
