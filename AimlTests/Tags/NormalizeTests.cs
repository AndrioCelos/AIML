using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class NormalizeTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Config.NormalSubstitutions.Add(new(" foo ", " bar "));
		var tag = new Normalize(new("foo"));
		Assert.AreEqual("bar", tag.Evaluate(test.RequestProcess).ToString());
	}
}
