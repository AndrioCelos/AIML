using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class IDTests {
	[Test]
	public void Evaluate() {
		var tag = new ID();
		Assert.AreEqual("tester", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
